using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Content.Server.Maps;
using Robust.Shared.ContentPack;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;

namespace Content.Server._Floof.Maps;

/// <summary>
///     Floof-specific copy of <see cref="MapMigrationSystem"/> that reads its own migration file,
///     so fork renames don't have to live in upstream's migration.yml.
///     Runs after the upstream system, so entries here take precedence over migration.yml.
/// </summary>
public sealed partial class FloofMapMigrationSystem : EntitySystem
{
    [Dependency] private IResourceManager _resMan = default!;

    private const string MigrationFile = "/Migrations/floofmigration.yml";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BeforeEntityReadEvent>(OnBeforeReadEvent, after: new[] { typeof(MapMigrationSystem) });

        #if DEBUG
        if (!TryReadFile(out var mappings))
            return;

        // Verify that all of the entries map to valid entity prototypes.
        foreach (var node in mappings.Children.Values)
        {
            var newId = ((ValueDataNode) node).Value;
            if (!string.IsNullOrEmpty(newId) && newId != "null")
                DebugTools.Assert(ProtoMan.HasIndex<EntityPrototype>(newId), $"{newId} is not an entity prototype.");
        }
        #endif
    }

    private bool TryReadFile([NotNullWhen(true)] out MappingDataNode? mappings)
    {
        mappings = null;
        var path = new ResPath(MigrationFile);
        if (!_resMan.TryContentFileRead(path, out var stream))
            return false;

        using var reader = new StreamReader(stream, EncodingHelpers.UTF8);
        var documents = DataNodeParser.ParseYamlStream(reader).FirstOrDefault();

        // An empty file parses to a null root.
        if (documents?.Root is not MappingDataNode root)
            return false;

        mappings = root;
        return true;
    }

    private void OnBeforeReadEvent(BeforeEntityReadEvent ev)
    {
        if (!TryReadFile(out var mappings))
            return;

        foreach (var (key, value) in mappings)
        {
            if (value is not ValueDataNode valueNode)
                continue;

            if (string.IsNullOrWhiteSpace(valueNode.Value) || valueNode.Value == "null")
            {
                ev.RenamedPrototypes.Remove(key);
                ev.DeletedPrototypes.Add(key);
            }
            else
            {
                // Indexer instead of Add, so overriding an upstream entry doesn't throw.
                ev.DeletedPrototypes.Remove(key);
                ev.RenamedPrototypes[key] = valueNode.Value;
            }
        }
    }
}

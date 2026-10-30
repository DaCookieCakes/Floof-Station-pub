using System.Numerics;
using Content.Server.Administration.Logs;
using Content.Server.Administration.Managers;
using Content.Server.Decals;
using Content.Shared._Floof.Decals;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Decals;

namespace Content.Server._Floof.Decals;

/// <summary>
/// Handles erase requests from the Floof decal placer.
/// </summary>
public sealed partial class FloofDecalEraseSystem : EntitySystem
{
    [Dependency] private IAdminManager _adminManager = default!;
    [Dependency] private IAdminLogManager _adminLogger = default!;
    [Dependency] private DecalEraseTargetSystem _eraseTargets = default!;
    [Dependency] private DecalSystem _decals = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<FloofRequestDecalEraseEvent>(OnEraseRequest);
    }

    private void OnEraseRequest(FloofRequestDecalEraseEvent ev, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;

        if (!_adminManager.HasAdminFlag(session, AdminFlags.Spawn))
            return;

        var coordinates = GetCoordinates(ev.Coordinates);

        if (!coordinates.IsValid(EntityManager))
            return;

        if (_transform.GetGrid(coordinates) is not { } grid)
            return;

        var localPos = Vector2.Transform(_transform.ToMapCoordinates(coordinates).Position, _transform.GetInvWorldMatrix(grid));

        // Only erase what the client asked for if it is actually near the click and passes the filter.
        var candidates = new Dictionary<DecalIndex, Decal>();
        foreach (var (index, decal) in _eraseTargets.GetCandidates(grid, localPos, ev.Filter))
        {
            candidates[index] = decal;
        }

        var removed = 0;
        foreach (var index in ev.Targets)
        {
            if (removed >= DecalEraseTargetSystem.MaxTargets)
                break;

            if (!candidates.Remove(index, out var decal))
                continue;

            removed++;

            if (session.AttachedEntity is { } actor)
            {
                _adminLogger.Add(LogType.CrayonDraw, LogImpact.Low,
                    $"{ToPrettyString(actor):actor} removed a {decal.Color} {decal.Id} at {ev.Coordinates}");
            }
            else
            {
                _adminLogger.Add(LogType.CrayonDraw, LogImpact.Low,
                    $"{session.Name} removed a {decal.Color} {decal.Id} at {ev.Coordinates}");
            }

            _decals.RemoveDecal(grid, index);
        }
    }
}

using Content.Shared.Decals;
using Robust.Shared.Prototypes;

namespace Content.Shared._Floof.Decals;

/// <summary>
/// A category shown as a tab in the Floof decal placer.
/// A decal belongs to this category if it matches any of <see cref="Decals"/>, <see cref="Tags"/> or <see cref="Prefixes"/>.
/// A decal may belong to several categories.
/// </summary>
[Prototype]
public sealed partial class DecalCategoryPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Locale id of the category name.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Higher priority categories are listed first.
    /// </summary>
    [DataField]
    public int Priority;

    /// <summary>
    /// Decals explicitly included in this category.
    /// </summary>
    [DataField]
    public List<ProtoId<DecalPrototype>> Decals = new();

    /// <summary>
    /// Decals with any of these <see cref="DecalPrototype.Tags"/> are included.
    /// </summary>
    [DataField]
    public List<string> Tags = new();

    /// <summary>
    /// Decals whose ID starts with any of these (case-sensitive) are included.
    /// </summary>
    [DataField]
    public List<string> Prefixes = new();

    /// <summary>
    /// Decals with any of these tags are excluded, even if matched by something else.
    /// </summary>
    [DataField]
    public List<string> ExcludeTags = new();

    /// <summary>
    /// Decals explicitly excluded from this category.
    /// </summary>
    [DataField]
    public List<ProtoId<DecalPrototype>> ExcludeDecals = new();

    public bool Contains(DecalPrototype decal)
    {
        foreach (var tag in decal.Tags)
        {
            if (ExcludeTags.Contains(tag))
                return false;
        }

        if (ExcludeDecals.Contains(decal.ID))
            return false;

        if (Decals.Contains(decal.ID))
            return true;

        foreach (var tag in decal.Tags)
        {
            if (Tags.Contains(tag))
                return true;
        }

        foreach (var prefix in Prefixes)
        {
            if (decal.ID.StartsWith(prefix, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}

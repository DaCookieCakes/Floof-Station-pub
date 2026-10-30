using Content.Shared.Decals;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._Floof.Decals;

[Serializable, NetSerializable]
public enum DecalEraseMode : byte
{
    /// <summary>
    /// Erase the single decal drawn on top under the cursor (highest draw depth, then most recently allocated).
    /// </summary>
    Topmost,

    /// <summary>
    /// Erase the single decal whose center is closest to the cursor.
    /// </summary>
    Nearest,

    /// <summary>
    /// Erase every decal near the cursor. This is the old WizDen behaviour.
    /// </summary>
    All,
}

/// <summary>
/// Optional filters applied before choosing which decals to erase.
/// </summary>
[Serializable, NetSerializable]
public readonly record struct DecalEraseFilter(string? DecalId, Color? Color);

/// <summary>
/// Sent by clients to erase decals using the Floof decal placer.
/// The client picks <see cref="Targets"/> since only it knows decal sizes;
/// the server checks they are near <see cref="Coordinates"/> and match <see cref="Filter"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed class FloofRequestDecalEraseEvent : EntityEventArgs
{
    public NetCoordinates Coordinates;
    public DecalEraseFilter Filter;
    public List<DecalIndex> Targets;

    public FloofRequestDecalEraseEvent(NetCoordinates coordinates, DecalEraseFilter filter, List<DecalIndex> targets)
    {
        Coordinates = coordinates;
        Filter = filter;
        Targets = targets;
    }
}

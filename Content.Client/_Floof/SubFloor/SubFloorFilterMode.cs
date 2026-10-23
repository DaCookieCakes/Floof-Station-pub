namespace Content.Client._Floof.SubFloor;

/// <summary>
///     Which subfloor entities show-subfloor mode reveals. Set with the subfloormode command or the sandbox window.
///     Separate from the T-ray's own modes so disposals can be split from atmos piping.
/// </summary>
public enum SubFloorFilterMode : byte
{
    All,
    Piping,
    Wiring,
    Disposals,
}

using Content.Client.Power.Visualizers;
using Content.Shared.Atmos.Components;
using Content.Shared.Disposal.Tube;

namespace Content.Client._Floof.SubFloor;

/// <summary>
///     Decides which subfloor entities pass the show-subfloor filter. Used by SubFloorHideSystem.
/// </summary>
public sealed partial class SubFloorFilterSystem : EntitySystem
{
    [Dependency] private EntityQuery<AtmosPipeLayersComponent> _pipeLayersQuery = default!;
    [Dependency] private EntityQuery<CableVisualizerComponent> _cableQuery = default!;
    [Dependency] private EntityQuery<DisposalTubeComponent> _disposalQuery = default!;

    /// <param name="pipeLayer">If set, only pipes on this layer match. Entities without pipe layers ignore it.</param>
    public bool Matches(EntityUid uid, SubFloorFilterMode mode, AtmosPipeLayer? pipeLayer)
    {
        var modeMatches = mode switch
        {
            SubFloorFilterMode.All => true,
            SubFloorFilterMode.Piping => _pipeLayersQuery.HasComp(uid),
            SubFloorFilterMode.Wiring => _cableQuery.HasComp(uid),
            SubFloorFilterMode.Disposals => _disposalQuery.HasComp(uid),
            _ => false,
        };

        if (!modeMatches)
            return false;

        return pipeLayer == null
               || !_pipeLayersQuery.TryComp(uid, out var layers)
               || layers.CurrentPipeLayer == pipeLayer;
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Client.Actions;
using Content.Client.Decals;
using Content.Shared._Floof.Decals;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Decals;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Placement;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._Floof.Decals;

/// <summary>
/// Floof replacement for <see cref="DecalPlacementSystem"/>, with fractional grid snapping and filtered erasing.
/// Placement still goes through the WizDen <see cref="RequestDecalPlacementEvent"/>.
/// </summary>
public sealed partial class FloofDecalPlacementSystem : EntitySystem
{
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IInputManager _inputManager = default!;
    [Dependency] private IOverlayManager _overlay = default!;
    [Dependency] private DecalEraseTargetSystem _eraseTargets = default!;
    [Dependency] private InputSystem _inputSystem = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedMapSystem _maps = default!;
    [Dependency] private SharedTransformSystem _transform = default!;
    [Dependency] private SpriteSystem _sprite = default!;

    /// <summary>
    /// Snap options offered in the UI, as subdivisions of a tile. 0 means no snapping.
    /// </summary>
    public static readonly int[] SnapDivisionOptions = { 0, 1, 2, 4, 8, 16, 32 };

    public string? DecalId { get; private set; }
    public Color DecalColor { get; private set; } = Color.White;
    public Angle DecalAngle { get; private set; } = Angle.Zero;
    public int ZIndex { get; private set; }
    public bool Cleanable { get; private set; }

    /// <summary>
    /// Snap to 1/n of a tile. 0 disables snapping.
    /// </summary>
    public int SnapDivisions { get; set; } = 1;

    public bool ShowGrid { get; set; } = true;

    public DecalEraseMode EraseMode { get; set; } = DecalEraseMode.Topmost;
    public bool EraseOnlySelected { get; set; }
    public bool EraseOnlyColor { get; set; }

    public bool Active { get; private set; }

    /// <summary>
    /// Raised when the rotate keybind is pressed, so the window can step its rotation field.
    /// </summary>
    public event Action? RotateRequested;

    private bool _placing;
    private bool _erasing;

    private readonly Dictionary<string, Vector2> _sizeCache = new();
    private readonly List<(DecalIndex Index, Decal Decal)> _targets = new();

    public override void Initialize()
    {
        base.Initialize();

        _overlay.AddOverlay(new FloofDecalPlacementOverlay(this, _transform, _sprite));

        CommandBinds.Builder
            .Bind(EngineKeyFunctions.EditorPlaceObject, new PointerStateInputCmdHandler(OnPlaceDown, OnPlaceUp, true))
            .Bind(EngineKeyFunctions.EditorCancelPlace, new PointerStateInputCmdHandler(OnEraseDown, OnEraseUp, true))
            // Before the engine's entity placer, which always swallows the rotate key.
            .BindBefore(EngineKeyFunctions.EditorRotateObject, new PointerStateInputCmdHandler(OnRotateDown, OnRotateUp, true), typeof(PlacementManager))
            .Register<FloofDecalPlacementSystem>();

        SubscribeLocalEvent<FillActionSlotEvent>(OnFillSlot);
        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypesReloaded);
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        if (args.WasModified<DecalPrototype>())
            _sizeCache.Clear();
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay<FloofDecalPlacementOverlay>();
        CommandBinds.Unregister<FloofDecalPlacementSystem>();
    }

    public void UpdateDecalInfo(string id, Color color, float rotation, int zIndex, bool cleanable)
    {
        DecalId = id;
        DecalColor = color;
        DecalAngle = Angle.FromDegrees(rotation);
        ZIndex = zIndex;
        Cleanable = cleanable;
    }

    public void SetActive(bool active)
    {
        Active = active;
        _placing = false;
        _erasing = false;

        if (active)
            _inputManager.Contexts.SetActiveContext("editor");
        else
            _inputSystem.SetEntityContextActive();
    }

    public DecalEraseFilter GetEraseFilter()
    {
        return new DecalEraseFilter(
            EraseOnlySelected ? DecalId : null,
            EraseOnlyColor ? DecalColor : null);
    }

    /// <summary>
    /// Size of a decal in tiles. The decal overlay draws textures at native size, so an 8x8 sprite is 1/4 of a tile.
    /// </summary>
    public Vector2 GetDecalSize(string id)
    {
        if (_sizeCache.TryGetValue(id, out var size))
            return size;

        size = ProtoMan.TryIndex<DecalPrototype>(id, out var proto)
            ? (Vector2) _sprite.Frame0(proto.Sprite).Size / EyeManager.PixelsPerMeter
            : Vector2.One;

        _sizeCache[id] = size;
        return size;
    }

    /// <summary>
    /// Gets the decals right-click would erase at a grid-local position.
    /// </summary>
    public void GetEraseTargets(EntityUid grid, Vector2 localPos, List<(DecalIndex Index, Decal Decal)> targets)
    {
        _eraseTargets.GetTargets(grid, localPos, EraseMode, GetEraseFilter(), targets, d => GetDecalSize(d.Id));
    }

    /// <summary>
    /// Gets the grid under a map position and that position in grid-local coordinates.
    /// </summary>
    public bool TryGetGridPosition(MapCoordinates mapPos, [NotNullWhen(true)] out EntityUid? grid, out Vector2 localPos)
    {
        grid = null;
        localPos = default;

        if (mapPos.MapId == MapId.Nullspace || !_maps.TryFindGridAt(mapPos, out var gridUid, out _))
            return false;

        grid = gridUid;
        localPos = Vector2.Transform(mapPos.Position, _transform.GetInvWorldMatrix(gridUid));
        return true;
    }

    public bool TryGetMouseGridPosition([NotNullWhen(true)] out EntityUid? grid, out Vector2 localPos)
    {
        var mapPos = _eye.PixelToMap(_inputManager.MouseScreenPosition);
        return TryGetGridPosition(mapPos, out grid, out localPos);
    }

    /// <summary>
    /// Gets the decal's bottom-left corner (what <see cref="Decal.Coordinates"/> stores) for a cursor position,
    /// with the cursor at the decal's center. Snapping aligns the decal edges to the 1/n grid;
    /// for a 32x32 decal at 1/1 this matches WizDen's "snap to tile".
    /// </summary>
    public Vector2 GetPlacementCorner(Vector2 localPos, Vector2 size)
    {
        var corner = localPos - size / 2f;

        if (SnapDivisions <= 0)
            return corner;

        return SnapToGrid(corner, SnapDivisions);
    }

    public static Vector2 SnapToGrid(Vector2 pos, int divisions)
    {
        return new Vector2(
            MathF.Round(pos.X * divisions, MidpointRounding.AwayFromZero) / divisions,
            MathF.Round(pos.Y * divisions, MidpointRounding.AwayFromZero) / divisions);
    }

    private bool OnPlaceDown(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        if (!Active || _placing || DecalId == null)
            return false;

        _placing = true;

        if (!TryGetGridPosition(_transform.ToMapCoordinates(coords), out var grid, out var localPos))
            return true;

        var corner = GetPlacementCorner(localPos, GetDecalSize(DecalId));
        var target = new EntityCoordinates(grid.Value, corner);

        var decal = new Decal(corner, DecalId, DecalColor, DecalAngle, ZIndex, Cleanable);
        RaisePredictiveEvent(new RequestDecalPlacementEvent(decal, GetNetCoordinates(target)));
        return true;
    }

    private bool OnPlaceUp(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        if (!Active)
            return false;

        _placing = false;
        return true;
    }

    private bool OnEraseDown(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        if (!Active || _erasing)
            return false;

        _erasing = true;

        if (!TryGetGridPosition(_transform.ToMapCoordinates(coords), out var grid, out var localPos))
            return true;

        GetEraseTargets(grid.Value, localPos, _targets);

        if (_targets.Count > 0)
        {
            var indices = new List<DecalIndex>(_targets.Count);
            foreach (var (index, _) in _targets)
            {
                indices.Add(index);
            }

            var target = new EntityCoordinates(grid.Value, localPos);
            RaiseNetworkEvent(new FloofRequestDecalEraseEvent(GetNetCoordinates(target), GetEraseFilter(), indices));
        }

        _targets.Clear();
        return true;
    }

    private bool OnEraseUp(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        if (!Active)
            return false;

        _erasing = false;
        return true;
    }

    private bool OnRotateDown(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        if (!Active)
            return false;

        RotateRequested?.Invoke();
        return true;
    }

    private bool OnRotateUp(ICommonSession? session, EntityCoordinates coords, EntityUid uid)
    {
        return Active;
    }

    /// <summary>
    /// Lets the selected decal be dragged onto the hotbar, same as the WizDen placer.
    /// Hotbar actions only support tile snapping, so fractional snaps become "no snap",
    /// and they assume 32x32 decals.
    /// </summary>
    private void OnFillSlot(FillActionSlotEvent ev)
    {
        if (!Active || _placing || ev.Action != null)
            return;

        if (DecalId == null || !ProtoMan.TryIndex<DecalPrototype>(DecalId, out var decalProto))
            return;

        var actionEvent = new PlaceDecalActionEvent
        {
            DecalId = DecalId,
            Color = DecalColor,
            Rotation = DecalAngle.Degrees,
            Snap = SnapDivisions == 1,
            ZIndex = ZIndex,
            Cleanable = Cleanable,
        };

        var actionId = Spawn(DecalPlacementSystem.DecalAction);
        var ent = (actionId, Comp<ActionComponent>(actionId));
        _actions.SetEvent(actionId, actionEvent);
        _actions.SetIcon(ent, decalProto.Sprite);
        _actions.SetIconColor(ent, DecalColor);

        _metaData.SetEntityName(actionId, $"{DecalId} ({DecalColor.ToHex()}, {(int) DecalAngle.Degrees})");

        ev.Action = actionId;
    }
}

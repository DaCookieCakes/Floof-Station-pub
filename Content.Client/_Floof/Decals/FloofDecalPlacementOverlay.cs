using System.Numerics;
using Content.Shared._Floof.Decals;
using Content.Shared.Decals;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;

namespace Content.Client._Floof.Decals;

/// <summary>
/// Draws the snap grid, the decals that right-click would erase, and the placement preview.
/// </summary>
public sealed partial class FloofDecalPlacementOverlay : Overlay
{
    [Dependency] private IPrototypeManager _prototype = default!;

    private readonly FloofDecalPlacementSystem _placement;
    private readonly SharedTransformSystem _transform;
    private readonly SpriteSystem _sprite;

    private readonly List<(DecalIndex Index, Decal Decal)> _targets = new();

    /// <summary>
    /// How many tiles around the cursor the grid is drawn.
    /// </summary>
    private const float GridRadius = 3f;

    private static readonly Color TileLineColor = Color.White.WithAlpha(0.45f);
    private static readonly Color SubLineColor = Color.White.WithAlpha(0.18f);
    private static readonly Color EraseColor = Color.Red;
    private static readonly Color SnapPointColor = Color.Yellow.WithAlpha(0.8f);

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    public FloofDecalPlacementOverlay(
        FloofDecalPlacementSystem placement,
        SharedTransformSystem transform,
        SpriteSystem sprite)
    {
        IoCManager.InjectDependencies(this);
        _placement = placement;
        _transform = transform;
        _sprite = sprite;
        ZIndex = 1000;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return _placement.Active;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_placement.TryGetMouseGridPosition(out var grid, out var localPos))
            return;

        if (_transform.GetMapId(grid.Value) != args.MapId)
            return;

        var handle = args.WorldHandle;
        handle.SetTransform(_transform.GetWorldMatrix(grid.Value));

        if (_placement.ShowGrid)
            DrawGrid(handle, localPos, Math.Max(1, _placement.SnapDivisions));

        DrawEraseTargets(handle, grid.Value, localPos);
        DrawPreview(handle, localPos);

        handle.SetTransform(Matrix3x2.Identity);
    }

    private void DrawGrid(DrawingHandleWorld handle, Vector2 center, int divisions)
    {
        var step = 1f / divisions;

        var minX = (int) MathF.Floor((center.X - GridRadius) * divisions);
        var maxX = (int) MathF.Ceiling((center.X + GridRadius) * divisions);
        var minY = (int) MathF.Floor((center.Y - GridRadius) * divisions);
        var maxY = (int) MathF.Ceiling((center.Y + GridRadius) * divisions);

        var bottom = center.Y - GridRadius;
        var top = center.Y + GridRadius;
        var left = center.X - GridRadius;
        var right = center.X + GridRadius;

        for (var i = minX; i <= maxX; i++)
        {
            var x = i * step;
            var color = GetLineColor(i, divisions, x - center.X);
            handle.DrawLine(new Vector2(x, bottom), new Vector2(x, top), color);
        }

        for (var i = minY; i <= maxY; i++)
        {
            var y = i * step;
            var color = GetLineColor(i, divisions, y - center.Y);
            handle.DrawLine(new Vector2(left, y), new Vector2(right, y), color);
        }
    }

    private static Color GetLineColor(int index, int divisions, float offset)
    {
        var color = index % divisions == 0 ? TileLineColor : SubLineColor;
        var fade = Math.Clamp(1f - MathF.Abs(offset) / GridRadius, 0f, 1f);
        return color.WithAlpha(color.A * fade);
    }

    private void DrawEraseTargets(DrawingHandleWorld handle, EntityUid grid, Vector2 localPos)
    {
        _placement.GetEraseTargets(grid, localPos, _targets);

        foreach (var (_, decal) in _targets)
        {
            var size = _placement.GetDecalSize(decal.Id);
            var box = new Box2Rotated(Box2.FromDimensions(decal.Coordinates, size), decal.Angle, decal.Coordinates + size / 2f);
            handle.DrawRect(box, EraseColor.WithAlpha(0.15f));
            handle.DrawRect(box, EraseColor, false);
        }

        _targets.Clear();
    }

    private void DrawPreview(DrawingHandleWorld handle, Vector2 localPos)
    {
        if (_placement.DecalId is not { } id || !_prototype.TryIndex<DecalPrototype>(id, out var decal))
            return;

        var size = _placement.GetDecalSize(id);
        var corner = _placement.GetPlacementCorner(localPos, size);
        var center = corner + size / 2f;
        var box = new Box2Rotated(Box2.FromDimensions(corner, size), _placement.DecalAngle, center);

        handle.DrawTextureRect(_sprite.Frame0(decal.Sprite), box, _placement.DecalColor);

        if (_placement.SnapDivisions > 0)
            handle.DrawCircle(center, 0.04f, SnapPointColor);
    }
}

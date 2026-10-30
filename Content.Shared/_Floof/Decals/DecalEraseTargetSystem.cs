using System.Numerics;
using Content.Shared.Decals;

namespace Content.Shared._Floof.Decals;

/// <summary>
/// Picks which decals an erase request should remove.
/// Decal sizes come from their textures, so only the client can pick targets accurately;
/// the server uses <see cref="GetCandidates"/> to validate what the client asked for.
/// </summary>
public sealed partial class DecalEraseTargetSystem : EntitySystem
{
    [Dependency] private SharedDecalSystem _decals = default!;

    /// <summary>
    /// Same range the WizDen decal eraser uses, measured from the decal center.
    /// </summary>
    public const float EraseRange = 0.75f;

    /// <summary>
    /// How far (in tiles) a decal's corner can be from the cursor and still be considered.
    /// Covers decals up to a few tiles in size.
    /// </summary>
    public const float MaxDecalReach = 3f;

    /// <summary>
    /// Max decals a single erase request can remove.
    /// </summary>
    public const int MaxTargets = 64;

    private const float ColorTolerance = 0.01f;

    /// <summary>
    /// Every decal near <paramref name="localPos"/> (grid-local) that passes the filter.
    /// </summary>
    public HashSet<(DecalIndex Index, Decal Decal)> GetCandidates(EntityUid grid, Vector2 localPos, DecalEraseFilter filter)
    {
        var reach = new Vector2(MaxDecalReach);
        var candidates = _decals.GetDecalsIntersecting(grid, new Box2(localPos - reach, localPos + reach));
        candidates.RemoveWhere(x => !MatchesFilter(x.Decal, filter));
        return candidates;
    }

    /// <summary>
    /// Gets the decals that would be erased at <paramref name="localPos"/> (grid-local coordinates).
    /// </summary>
    /// <param name="getSize">Decal size in tiles. Defaults to 1x1.</param>
    public void GetTargets(
        EntityUid grid,
        Vector2 localPos,
        DecalEraseMode mode,
        DecalEraseFilter filter,
        List<(DecalIndex Index, Decal Decal)> targets,
        Func<Decal, Vector2>? getSize = null)
    {
        targets.Clear();

        var candidates = GetCandidates(grid, localPos, filter);
        Vector2 Size(Decal decal) => getSize?.Invoke(decal) ?? Vector2.One;

        switch (mode)
        {
            case DecalEraseMode.All:
                foreach (var candidate in candidates)
                {
                    if (targets.Count >= MaxTargets)
                        break;

                    if (CenterDistance(candidate.Decal, Size(candidate.Decal), localPos) <= EraseRange)
                        targets.Add(candidate);
                }
                break;

            case DecalEraseMode.Nearest:
            {
                (DecalIndex Index, Decal Decal)? best = null;
                var bestDistance = float.MaxValue;

                foreach (var candidate in candidates)
                {
                    var distance = CenterDistance(candidate.Decal, Size(candidate.Decal), localPos);
                    if (distance > EraseRange)
                        continue;

                    // Ties go to the one drawn on top.
                    if (distance < bestDistance ||
                        best != null && MathHelper.CloseTo(distance, bestDistance) && CompareDrawOrder(candidate, best.Value) > 0)
                    {
                        best = candidate;
                        bestDistance = distance;
                    }
                }

                if (best != null)
                    targets.Add(best.Value);
                break;
            }

            case DecalEraseMode.Topmost:
            {
                (DecalIndex Index, Decal Decal)? best = null;

                foreach (var candidate in candidates)
                {
                    if (!HitTest(candidate.Decal, Size(candidate.Decal), localPos))
                        continue;

                    if (best == null || CompareDrawOrder(candidate, best.Value) > 0)
                        best = candidate;
                }

                if (best != null)
                    targets.Add(best.Value);
                break;
            }
        }
    }

    public static bool MatchesFilter(Decal decal, DecalEraseFilter filter)
    {
        if (filter.DecalId != null && decal.Id != filter.DecalId)
            return false;

        if (filter.Color is { } color)
        {
            var decalColor = decal.Color ?? Color.White;
            if (MathF.Abs(decalColor.R - color.R) > ColorTolerance ||
                MathF.Abs(decalColor.G - color.G) > ColorTolerance ||
                MathF.Abs(decalColor.B - color.B) > ColorTolerance ||
                MathF.Abs(decalColor.A - color.A) > ColorTolerance)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Center of a decal. <see cref="Decal.Coordinates"/> is its bottom-left corner.
    /// </summary>
    public static Vector2 GetCenter(Decal decal, Vector2 size)
    {
        return decal.Coordinates + size / 2f;
    }

    /// <summary>
    /// Whether the point is inside the decal's (rotated) box.
    /// </summary>
    public static bool HitTest(Decal decal, Vector2 size, Vector2 localPos)
    {
        var relative = (-decal.Angle).RotateVec(localPos - GetCenter(decal, size));
        return MathF.Abs(relative.X) <= size.X / 2f && MathF.Abs(relative.Y) <= size.Y / 2f;
    }

    private static float CenterDistance(Decal decal, Vector2 size, Vector2 localPos)
    {
        return (localPos - GetCenter(decal, size)).Length();
    }

    /// <summary>
    /// Same ordering the decal overlay draws with; greater means drawn later (on top).
    /// </summary>
    public static int CompareDrawOrder((DecalIndex Index, Decal Decal) x, (DecalIndex Index, Decal Decal) y)
    {
        var zComp = x.Decal.ZIndex.CompareTo(y.Decal.ZIndex);
        if (zComp != 0)
            return zComp;

        var xComp = x.Index.Chunk.X.CompareTo(y.Index.Chunk.X);
        if (xComp != 0)
            return xComp;

        var yComp = x.Index.Chunk.Y.CompareTo(y.Index.Chunk.Y);
        if (yComp != 0)
            return yComp;

        return x.Index.Id.CompareTo(y.Index.Id);
    }
}

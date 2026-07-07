//Minigame: Spell Drawing


using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure math utility that scores how closely a player's drawn stroke matches a spell's
/// template shape (a simplified $1 Unistroke Recognizer). Static, not a MonoBehaviour —
/// no Unity lifecycle needed, and it keeps the algorithm unit-testable outside Play mode.
///
/// Pipeline: Resample (equal distance) -> Center -> Uniform Scale -> compare
/// forwards/reversed against the template -> convert distance to a percentage.
///
/// This class has no opinion about what counts as "success" — it only returns a raw
/// 0-100 similarity percentage. SpellManager owns the 80% threshold decision.
/// </summary>
public static class ShapeComparer
{
    // Number of points every shape (drawn or template) is resampled to before comparing.
    // 64 matches the plan's target and gives a smooth-enough curve without being expensive.
    private const int DefaultResampleCount = 64;

    // Because both shapes are centered at (0,0) and uniformly scaled so their longest
    // dimension fills a 1x1 box, no point can lie outside the box spanning
    // (-0.5,-0.5) to (0.5,0.5). The furthest two points can ever be from each other is the
    // diagonal of that box: sqrt(0.5^2 + 0.5^2) * 2 = sqrt(2). That bounds the maximum
    // possible average per-point distance, which is what turns raw distance into a percentage.
    // Tune this after playtesting real drawings if scores feel too generous/harsh.
    private const float MaxPossibleDistance = 1.41421356f; // sqrt(2)

    /// <summary>
    /// Compares a freshly drawn stroke against a spell's template and returns a similarity
    /// score from 0 to 100. Handles normalizing the drawn points every call (cheap, since it's
    /// a single stroke) and lazily normalizing + caching the spell's template points on the
    /// SpellData asset itself (expensive-once, since the template never changes at runtime).
    /// </summary>
    public static float CompareShape(List<Vector2> drawnPoints, SpellData spellData, int resampleCount = DefaultResampleCount)
    {
        if (drawnPoints == null || drawnPoints.Count < 2)
        {
            Debug.LogWarning("ShapeComparer: drawnPoints needs at least 2 points to compare.");
            return 0f;
        }

        if (spellData == null || spellData.TemplatePoints == null || spellData.TemplatePoints.Count < 2)
        {
            Debug.LogWarning("ShapeComparer: spellData is missing a valid template shape.");
            return 0f;
        }

        List<Vector2> normalizedDrawn = NormalizeShape(drawnPoints, resampleCount);

        if (!spellData.HasCachedNormalizedPoints)
        {
            List<Vector2> normalizedTemplate = NormalizeShape(new List<Vector2>(spellData.TemplatePoints), resampleCount);
            List<Vector2> reversedTemplate = NormalizeShape(ReversePoints(spellData.TemplatePoints), resampleCount);
            spellData.SetCachedNormalizedPoints(normalizedTemplate, reversedTemplate);
        }

        // Direction fix: compare against both the template and its reversed copy,
        // keep whichever gives the lower average distance (better score).
        float forwardDistance = AverageDistance(normalizedDrawn, spellData.CachedNormalizedPoints);
        float reversedDistance = AverageDistance(normalizedDrawn, spellData.CachedReversedNormalizedPoints);
        float bestDistance = Mathf.Min(forwardDistance, reversedDistance);

        return DistanceToPercentage(bestDistance);
    }

    /// <summary>
    /// Runs a raw list of points through the full normalization pipeline:
    /// resample to a fixed point count, center on (0,0), then uniformly scale
    /// so the shape's longest dimension fills a 1x1 box.
    /// </summary>
    private static List<Vector2> NormalizeShape(List<Vector2> points, int resampleCount)
    {
        List<Vector2> resampled = ResampleByDistance(points, resampleCount);
        List<Vector2> centered = CenterOnOrigin(resampled);
        List<Vector2> scaled = ScaleUniformly(centered);
        return scaled;
    }

    /// <summary>
    /// Walks the path formed by the given points and places <paramref name="targetCount"/>
    /// points at equal *distance* intervals along the total path length (not equal time
    /// intervals). This makes a fast drag and a slow, careful drag resample to the same
    /// number of comparably-spaced points.
    /// </summary>
    private static List<Vector2> ResampleByDistance(List<Vector2> points, int targetCount)
    {
        var result = new List<Vector2>(targetCount);

        // Degenerate case: every point is (near) identical (e.g. a stray tap that
        // slipped past _minPointsForValidDrawing). Avoid divide-by-zero below.
        float totalLength = PathLength(points);
        if (totalLength <= Mathf.Epsilon)
        {
            for (int i = 0; i < targetCount; i++)
                result.Add(points[0]);
            return result;
        }

        float interval = totalLength / (targetCount - 1);
        float distanceSinceLastPoint = 0f;

        result.Add(points[0]);
        int srcIndex = 0;
        Vector2 currentPoint = points[0];

        while (result.Count < targetCount)
        {
            if (srcIndex >= points.Count - 1)
            {
                // Floating point rounding can leave us just short — pad with the final point.
                result.Add(points[points.Count - 1]);
                continue;
            }

            Vector2 nextPoint = points[srcIndex + 1];
            float segmentLength = Vector2.Distance(currentPoint, nextPoint);

            if (distanceSinceLastPoint + segmentLength >= interval)
            {
                // The next resampled point lands somewhere along this segment — interpolate.
                float remaining = interval - distanceSinceLastPoint;
                float t = remaining / segmentLength;
                Vector2 newPoint = Vector2.Lerp(currentPoint, nextPoint, t);

                result.Add(newPoint);
                currentPoint = newPoint;
                distanceSinceLastPoint = 0f;
                // Don't advance srcIndex — the remainder of this segment might contain
                // the *next* resampled point too, if interval is small relative to segmentLength.
            }
            else
            {
                distanceSinceLastPoint += segmentLength;
                currentPoint = nextPoint;
                srcIndex++;
            }
        }

        return result;
    }

    private static float PathLength(List<Vector2> points)
    {
        float length = 0f;
        for (int i = 1; i < points.Count; i++)
            length += Vector2.Distance(points[i - 1], points[i]);
        return length;
    }

    /// <summary>
    /// Shifts every point so the bounding box of the shape is centered on (0,0).
    /// </summary>
    private static List<Vector2> CenterOnOrigin(List<Vector2> points)
    {
        Rect bounds = GetBoundingBox(points);
        Vector2 center = bounds.center;

        var result = new List<Vector2>(points.Count);
        for (int i = 0; i < points.Count; i++)
            result.Add(points[i] - center);

        return result;
    }

    /// <summary>
    /// Scales the shape by a single uniform factor (same for x and y) so its longest
    /// bounding-box dimension fills a 1x1 box. Preserves aspect ratio, unlike the original
    /// $1 Recognizer's non-uniform scaling — deliberate, since a tall oval shouldn't
    /// normalize into looking identical to a circle for a spell-shape game.
    /// </summary>
    private static List<Vector2> ScaleUniformly(List<Vector2> points)
    {
        Rect bounds = GetBoundingBox(points);
        float longestDimension = Mathf.Max(bounds.width, bounds.height);

        if (longestDimension <= Mathf.Epsilon)
            return points; // Degenerate shape (a single point) — nothing to scale.

        float scaleFactor = 1f / longestDimension;

        var result = new List<Vector2>(points.Count);
        for (int i = 0; i < points.Count; i++)
            result.Add(points[i] * scaleFactor);

        return result;
    }

    private static Rect GetBoundingBox(List<Vector2> points)
    {
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        for (int i = 0; i < points.Count; i++)
        {
            Vector2 p = points[i];
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }

        return new Rect(minX, minY, maxX - minX, maxY - minY);
    }

    private static List<Vector2> ReversePoints(IReadOnlyList<Vector2> points)
    {
        var result = new List<Vector2>(points.Count);
        for (int i = points.Count - 1; i >= 0; i--)
            result.Add(points[i]);
        return result;
    }

    /// <summary>
    /// Walks two equally-sized, already-normalized point lists together and averages the
    /// distance between corresponding points (1-to-1, 2-to-2, ... N-to-N).
    /// </summary>
    private static float AverageDistance(List<Vector2> a, IReadOnlyList<Vector2> b)
    {
        int count = Mathf.Min(a.Count, b.Count);
        if (count == 0) return MaxPossibleDistance; // Nothing to compare — treat as a total miss.

        float totalDistance = 0f;
        for (int i = 0; i < count; i++)
            totalDistance += Vector2.Distance(a[i], b[i]);

        return totalDistance / count;
    }

    private static float DistanceToPercentage(float averageDistance)
    {
        float score = 1f - (averageDistance / MaxPossibleDistance);
        return Mathf.Clamp01(score) * 100f;
    }
}
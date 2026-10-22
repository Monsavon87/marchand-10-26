using UnityEngine;

/// <summary>
/// Pure helpers on a path stored as a polyline (an array of points and a count).
/// Shared by the combat manager (budget) and the path preview (in range / out of range).
/// </summary>
public static class PathMath
{
    /// <summary>Total length of the polyline.</summary>
    public static float Length(Vector3[] points, int count)
    {
        float length = 0f;
        for (int i = 1; i < count; i++)
        {
            length += Vector3.Distance(points[i - 1], points[i]);
        }

        return length;
    }

    /// <summary>
    /// Cuts the polyline at 'distance' from its start: the points beyond are dropped and the last kept point
    /// becomes the cut point. Returns the new point count. A path shorter than 'distance' is left unchanged.
    /// </summary>
    public static int TruncateAt(Vector3[] points, int count, float distance)
    {
        if (count < 2)
        {
            return count;
        }

        float walked = 0f;
        for (int i = 1; i < count; i++)
        {
            float segment = Vector3.Distance(points[i - 1], points[i]);
            if (walked + segment >= distance)
            {
                float t = segment > 0f ? (distance - walked) / segment : 0f;
                points[i] = Vector3.Lerp(points[i - 1], points[i], Mathf.Clamp01(t));
                return i + 1;
            }

            walked += segment;
        }

        return count;
    }

    /// <summary>
    /// Splits the polyline at 'distance' from its start. 'first' receives the part from the start to the cut point,
    /// 'rest' the part from the cut point to the end. If the path is shorter than 'distance', everything goes into
    /// 'first' and 'rest' is empty. Both arrays must be at least as large as the source.
    /// </summary>
    public static void SplitAt(Vector3[] points, int count, float distance, Vector3[] first, out int firstCount, Vector3[] rest, out int restCount)
    {
        firstCount = 0;
        restCount = 0;
        if (count == 0)
        {
            return;
        }

        float walked = 0f;
        int cutSegment = -1;
        Vector3 cut = points[count - 1];
        for (int i = 1; i < count; i++)
        {
            float segment = Vector3.Distance(points[i - 1], points[i]);
            if (walked + segment >= distance)
            {
                float t = segment > 0f ? (distance - walked) / segment : 0f;
                cut = Vector3.Lerp(points[i - 1], points[i], Mathf.Clamp01(t));
                cutSegment = i;
                break;
            }

            walked += segment;
        }

        if (cutSegment < 0)
        {
            for (int i = 0; i < count; i++)
            {
                first[firstCount++] = points[i];
            }

            return;
        }

        for (int i = 0; i < cutSegment; i++)
        {
            first[firstCount++] = points[i];
        }

        first[firstCount++] = cut;

        rest[restCount++] = cut;
        for (int i = cutSegment; i < count; i++)
        {
            rest[restCount++] = points[i];
        }
    }
}

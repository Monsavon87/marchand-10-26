using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Draws random points on the NavMesh that a unit can really walk to: the base of the wandering bands and of the
/// routes made for a band that comes back. The dice is the caller's, so a seed gives the same draws. The NavMeshPath is
/// the caller's too (reused: no allocation per draw).
/// </summary>
public static class NavMeshPicker
{
    private const float SampleRadius = 4f;

    /// <summary>True when a complete path leads from one point to the other.</summary>
    public static bool IsReachable(Vector3 from, Vector3 to, NavMeshPath path)
    {
        return NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
    }

    /// <summary>
    /// One try: a point drawn between minDistance and radius around the origin (flat distance), put on the NavMesh and
    /// reachable from the origin. False when this draw did not give a valid point.
    /// </summary>
    public static bool TryPickAround(Vector3 origin, float radius, float minDistance, System.Random dice, NavMeshPath path, out Vector3 point)
    {
        float angle = (float)(dice.NextDouble() * Mathf.PI * 2.0);
        float distance = Mathf.Lerp(minDistance, radius, Mathf.Sqrt((float)dice.NextDouble()));
        Vector3 candidate = origin + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;
        point = origin;
        if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, SampleRadius, NavMesh.AllAreas) || !IsReachable(origin, hit.position, path))
        {
            return false;
        }

        point = hit.position;
        return FlatDistance(origin, point) >= minDistance * 0.5f;
    }

    /// <summary>One try: a point drawn anywhere in the bounds (seen from above), on the NavMesh and reachable from 'reachableFrom'.</summary>
    public static bool TryPickInBounds(Bounds bounds, Vector3 reachableFrom, System.Random dice, NavMeshPath path, out Vector3 point)
    {
        var candidate = new Vector3(
            Mathf.Lerp(bounds.min.x, bounds.max.x, (float)dice.NextDouble()),
            bounds.center.y,
            Mathf.Lerp(bounds.min.z, bounds.max.z, (float)dice.NextDouble()));
        point = candidate;
        if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, SampleRadius, NavMesh.AllAreas) || !IsReachable(reachableFrom, hit.position, path))
        {
            return false;
        }

        point = hit.position;
        return true;
    }

    /// <summary>Distance from a point to the nearest side of the bounds (seen from above); negative outside.</summary>
    public static float DistanceToEdge(Bounds bounds, Vector3 point)
    {
        return Mathf.Min(
            Mathf.Min(point.x - bounds.min.x, bounds.max.x - point.x),
            Mathf.Min(point.z - bounds.min.z, bounds.max.z - point.z));
    }

    /// <summary>
    /// One wandering hop: a point reachable from the origin, between minHop and radius away, never closer than
    /// edgeMargin to the edge of the map. When the origin is within pullZone of the edge, up to pullSamples valid
    /// points are drawn and the one nearest the center of the map is taken (a band drifting to the border is brought
    /// back); elsewhere the first valid point is taken. At most 'tries' draws. False when none was valid.
    /// </summary>
    public static bool TryPickWanderHop(Vector3 origin, float radius, float minHop, Bounds map, float edgeMargin, float pullZone, int pullSamples,
        int tries, System.Random dice, NavMeshPath path, out Vector3 point)
    {
        bool pulled = DistanceToEdge(map, origin) < pullZone;
        int wanted = pulled ? Mathf.Max(1, pullSamples) : 1;
        int found = 0;
        point = origin;
        float bestToCenter = float.MaxValue;
        for (int i = 0; i < tries && found < wanted; i++)
        {
            if (!TryPickAround(origin, radius, minHop, dice, path, out Vector3 candidate) || DistanceToEdge(map, candidate) < edgeMargin)
            {
                continue;
            }

            found++;
            float toCenter = FlatDistance(candidate, map.center);
            if (toCenter < bestToCenter)
            {
                bestToCenter = toCenter;
                point = candidate;
            }
        }

        return found > 0;
    }

    /// <summary>
    /// A route of 'count' points around the origin, each at least minSpacing from the one before, reachable from the
    /// origin and (when the map has a size) at least edgeMargin from its edge, written to 'result' (emptied first).
    /// False when it could not be made in 'tries' draws per point.
    /// </summary>
    public static bool TryPickRoute(Vector3 origin, int count, float radius, float minSpacing, int tries, System.Random dice, NavMeshPath path,
        List<Vector3> result, Bounds map, float edgeMargin)
    {
        result.Clear();
        Vector3 previous = origin;
        for (int i = 0; i < count; i++)
        {
            bool found = false;
            for (int t = 0; t < tries && !found; t++)
            {
                if (TryPickAround(origin, radius, 0f, dice, path, out Vector3 point) && FlatDistance(previous, point) >= minSpacing
                    && (map.size == Vector3.zero || DistanceToEdge(map, point) >= edgeMargin))
                {
                    result.Add(point);
                    previous = point;
                    found = true;
                }
            }

            if (!found)
            {
                result.Clear();
                return false;
            }
        }

        return true;
    }

    /// <summary>The bounds of the NavMesh, to draw points anywhere on the map. False when there is no NavMesh.</summary>
    public static bool TryGetNavMeshBounds(out Bounds bounds)
    {
        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();
        bounds = default;
        if (triangulation.vertices == null || triangulation.vertices.Length == 0)
        {
            return false;
        }

        bounds = new Bounds(triangulation.vertices[0], Vector3.zero);
        for (int i = 1; i < triangulation.vertices.Length; i++)
        {
            bounds.Encapsulate(triangulation.vertices[i]);
        }

        return true;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}

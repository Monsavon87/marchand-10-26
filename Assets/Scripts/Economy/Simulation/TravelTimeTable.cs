using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// The game minutes the caravan takes to walk from one town's entrance to another's, worked out once on the NavMesh
/// of the map: the path the pathfinder would choose with the terrain costs of TerrainSettings, walked at the base
/// speed slowed by the terrain it crosses (a slow terrain makes the trip longer on the calendar). The table is indexed
/// like the list of markets it was built from. No unit moves: nothing in the scene changes.
/// </summary>
public class TravelTimeTable
{
    private const float SampleStep = 2f;
    private const float SnapRadius = 5f;

    private readonly float[,] minutes;
    private readonly float[,] meters;

    private TravelTimeTable(int count)
    {
        minutes = new float[count, count];
        meters = new float[count, count];
    }

    /// <summary>Game minutes of the trip, or infinity when no path joins the two towns.</summary>
    public float Minutes(int from, int to)
    {
        return minutes[from, to];
    }

    /// <summary>Length of the path in meters, or infinity when none.</summary>
    public float Meters(int from, int to)
    {
        return meters[from, to];
    }

    /// <summary>Builds the table. Logs an error for each pair of towns that no path joins.</summary>
    public static TravelTimeTable Build(IReadOnlyList<Market> markets, TerrainSettings terrain, float minutesPerMeter)
    {
        var table = new TravelTimeTable(markets.Count);
        NavMeshQueryFilter filter = new NavMeshQueryFilter { agentTypeID = 0, areaMask = NavMesh.AllAreas };
        var speedByArea = new Dictionary<int, float>();
        if (terrain != null)
        {
            for (int i = 0; i < terrain.Entries.Count; i++)
            {
                TerrainSettings.TerrainEntry entry = terrain.Entries[i];
                int area = NavMesh.GetAreaFromName(entry.areaName);
                if (area < 0)
                {
                    Debug.LogError("TravelTimeTable: the terrain area '" + entry.areaName + "' does not exist on the NavMesh.");
                    continue;
                }

                filter.SetAreaCost(area, entry.pathCost);
                speedByArea[area] = entry.speedMultiplier > 0f ? entry.speedMultiplier : 1f;
            }
        }

        var path = new NavMeshPath();
        for (int from = 0; from < markets.Count; from++)
        {
            for (int to = 0; to < markets.Count; to++)
            {
                if (from == to)
                {
                    continue;
                }

                bool found = TryMeasure(markets[from].Town.Entrance, markets[to].Town.Entrance, filter, path, speedByArea,
                    minutesPerMeter, out float length, out float time);
                table.meters[from, to] = found ? length : float.PositiveInfinity;
                table.minutes[from, to] = found ? time : float.PositiveInfinity;
                if (!found)
                {
                    Debug.LogError("TravelTimeTable: no path from " + markets[from].TownName + " to " + markets[to].TownName + ".");
                }
            }
        }

        return table;
    }

    private static bool TryMeasure(Vector3 start, Vector3 end, NavMeshQueryFilter filter, NavMeshPath path,
        Dictionary<int, float> speedByArea, float minutesPerMeter, out float length, out float time)
    {
        length = 0f;
        time = 0f;
        if (!NavMesh.SamplePosition(start, out NavMeshHit from, SnapRadius, filter)
            || !NavMesh.SamplePosition(end, out NavMeshHit to, SnapRadius, filter)
            || !NavMesh.CalculatePath(from.position, to.position, filter, path)
            || path.status != NavMeshPathStatus.PathComplete)
        {
            return false;
        }

        Vector3[] corners = path.corners;
        for (int i = 1; i < corners.Length; i++)
        {
            Vector3 a = corners[i - 1];
            Vector3 b = corners[i];
            float segment = Vector3.Distance(a, b);
            int steps = Mathf.Max(1, Mathf.CeilToInt(segment / SampleStep));
            float stepLength = segment / steps;
            for (int s = 0; s < steps; s++)
            {
                Vector3 middle = Vector3.Lerp(a, b, (s + 0.5f) / steps);
                float speed = 1f;
                if (NavMesh.SamplePosition(middle, out NavMeshHit hit, SampleStep, filter))
                {
                    speedByArea.TryGetValue(AreaIndex(hit.mask), out speed);
                    speed = speed > 0f ? speed : 1f;
                }

                length += stepLength;
                time += stepLength * minutesPerMeter / speed;
            }
        }

        return true;
    }

    // The area mask of a hit has one bit set: the index of the area.
    private static int AreaIndex(int mask)
    {
        int index = 0;
        while (mask > 1)
        {
            mask >>= 1;
            index++;
        }

        return index;
    }
}

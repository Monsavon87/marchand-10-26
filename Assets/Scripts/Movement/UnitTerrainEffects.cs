using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Applies the terrain rules from TerrainSettings to one unit: per-area path costs at startup,
/// and a speed multiplier that follows the terrain under the unit. Talks to UnitMover only.
/// </summary>
[RequireComponent(typeof(UnitMover))]
public class UnitTerrainEffects : MonoBehaviour
{
    private const int AreaCount = 32;
    private const float SampleDistance = 1f;

    [SerializeField] private UnitMover mover;
    [SerializeField] private TerrainSettings settings;
    [Tooltip("Time between two terrain checks while the unit moves, in seconds.")]
    [SerializeField] private float checkInterval = 0.1f;

    // Speed multiplier per NavMesh area index, filled once at startup (no allocation afterwards).
    private readonly float[] speedByArea = new float[AreaCount];
    private float nextCheckTime;

    /// <summary>NavMesh area index under the unit at the last check (-1 before the first check).</summary>
    public int CurrentArea { get; private set; } = -1;
    public float CurrentSpeedMultiplier { get; private set; } = 1f;

    private void Start()
    {
        if (mover == null || settings == null)
        {
            Debug.LogError("UnitTerrainEffects: mover or settings is not assigned.", this);
            enabled = false;
            return;
        }

        for (int i = 0; i < AreaCount; i++)
        {
            speedByArea[i] = 1f;
        }

        foreach (TerrainSettings.TerrainEntry entry in settings.Entries)
        {
            int area = NavMesh.GetAreaFromName(entry.areaName);
            if (area < 0)
            {
                Debug.LogError("UnitTerrainEffects: unknown NavMesh area '" + entry.areaName + "'.", this);
                continue;
            }

            speedByArea[area] = entry.speedMultiplier;
            mover.SetAreaCost(area, entry.pathCost);
        }
    }

    private void Update()
    {
        if (Time.time < nextCheckTime || !mover.IsMoving)
        {
            return;
        }

        nextCheckTime = Time.time + checkInterval;

        if (!NavMesh.SamplePosition(mover.GroundPosition, out NavMeshHit hit, SampleDistance, NavMesh.AllAreas))
        {
            return;
        }

        int area = AreaFromMask(hit.mask);
        if (area == CurrentArea)
        {
            return;
        }

        CurrentArea = area;
        CurrentSpeedMultiplier = speedByArea[area];
        mover.SetSpeedMultiplier(CurrentSpeedMultiplier);
    }

    // The hit mask has one bit set: the index of the area under the point.
    private static int AreaFromMask(int mask)
    {
        for (int i = 0; i < AreaCount; i++)
        {
            if ((mask & (1 << i)) != 0)
            {
                return i;
            }
        }

        return 0;
    }
}

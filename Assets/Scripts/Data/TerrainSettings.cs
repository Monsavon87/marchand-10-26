using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Terrain data: path cost and speed multiplier per NavMesh area.
/// Single source of truth, editable in the Inspector without touching the code.
/// </summary>
[CreateAssetMenu(fileName = "TerrainSettings", menuName = "Marchand/Terrain Settings")]
public class TerrainSettings : ScriptableObject
{
    [Serializable]
    public struct TerrainEntry
    {
        [Tooltip("Exact NavMesh area name. The built-in 'Walkable' area is the plain.")]
        public string areaName;
        [Tooltip("Path cost: the pathfinder prefers cheaper terrain, even if the route is longer.")]
        public float pathCost;
        [Tooltip("Movement speed multiplier applied while the unit stands on this terrain.")]
        public float speedMultiplier;
    }

    [SerializeField] private TerrainEntry[] entries = Array.Empty<TerrainEntry>();

    public IReadOnlyList<TerrainEntry> Entries => entries;
}

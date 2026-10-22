using System.Collections.Generic;
using UnityEngine;

/// <summary>The side a unit fights for.</summary>
public enum Team
{
    Player,
    Enemy
}

/// <summary>
/// Tells which side a unit belongs to, and keeps a registry of every enabled unit
/// so that AI code can find the units it must chase without scanning the whole scene.
/// </summary>
public class UnitTeam : MonoBehaviour
{
    private static readonly List<UnitTeam> AllUnits = new List<UnitTeam>();

    [SerializeField] private Team team = Team.Player;

    /// <summary>All enabled units of every team.</summary>
    public static IReadOnlyList<UnitTeam> All => AllUnits;

    public Team Team => team;

    private void OnEnable()
    {
        AllUnits.Add(this);
    }

    private void OnDisable()
    {
        AllUnits.Remove(this);
    }
}

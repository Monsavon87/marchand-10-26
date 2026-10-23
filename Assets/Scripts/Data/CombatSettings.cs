using UnityEngine;

/// <summary>
/// How a combat is run: which events pause the game by themselves, what the player's units can see, and how many
/// orders a unit may line up. Pure data, editable in the Inspector.
/// </summary>
[CreateAssetMenu(fileName = "CombatSettings", menuName = "Marchand/Combat Settings")]
public class CombatSettings : ScriptableObject
{
    [Header("Automatic pause")]
    [Tooltip("Pause the game when a unit of the player's side dies.")]
    [SerializeField] private bool autoPauseOnAllyDeath = true;
    [Tooltip("Pause the game when an enemy comes into view of one of the player's units.")]
    [SerializeField] private bool autoPauseOnEnemySighted = true;

    [Header("Vision of the player's units")]
    [Tooltip("A player unit sees an enemy closer than this (meters) if nothing solid is in the way.")]
    [Min(0f)] [SerializeField] private float viewRadius = 9f;
    [Tooltip("Solid objects on these layers block the view. Units never block it.")]
    [SerializeField] private LayerMask sightBlockers = ~0;
    [Tooltip("Height of the eyes above a unit's pivot, in meters.")]
    [SerializeField] private float eyeHeight = 0.5f;
    [Tooltip("Seconds between two checks of who sees whom.")]
    [Min(0.05f)] [SerializeField] private float sightCheckInterval = 0.2f;

    [Header("Orders")]
    [Tooltip("How many orders a unit can line up behind the one it is carrying out (Shift + click).")]
    [Min(0)] [SerializeField] private int maxQueuedOrders = 4;

    public bool AutoPauseOnAllyDeath => autoPauseOnAllyDeath;
    public bool AutoPauseOnEnemySighted => autoPauseOnEnemySighted;
    public float ViewRadius => viewRadius;
    public LayerMask SightBlockers => sightBlockers;
    public float EyeHeight => eyeHeight;
    public float SightCheckInterval => sightCheckInterval;
    public int MaxQueuedOrders => maxQueuedOrders;
}

using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// How a combat is run: which events pause the game by themselves, how sight works, how many orders a unit may line
/// up, and how arrows fly. Pure data, editable in the Inspector.
/// </summary>
[CreateAssetMenu(fileName = "CombatSettings", menuName = "Marchand/Combat Settings")]
public class CombatSettings : ScriptableObject
{
    [Header("Automatic pause")]
    [Tooltip("Pause the game when a unit of the player's side dies.")]
    [SerializeField] private bool autoPauseOnAllyDeath = true;
    [Tooltip("Pause the game when an enemy unit notices one of the player's units (once per enemy unit).")]
    [FormerlySerializedAs("autoPauseOnEnemySighted")]
    [SerializeField] private bool autoPauseOnEnemyDetection = true;

    [Header("Sight")]
    [Tooltip("Solid objects on these layers block the view and the shots. Units never block them.")]
    [SerializeField] private LayerMask sightBlockers = ~0;
    [Tooltip("Height of the eyes above a unit's pivot, in meters.")]
    [SerializeField] private float eyeHeight = 0.5f;

    [Header("Orders")]
    [Tooltip("How many orders a unit can line up behind the one it is carrying out (Shift + click).")]
    [Min(0)] [SerializeField] private int maxQueuedOrders = 4;

    [Header("Ranged attacks")]
    [Tooltip("The arrow that flies from an archer to its target. Without it, ranged units cannot shoot.")]
    [SerializeField] private Projectile projectilePrefab;
    [Tooltip("How fast an arrow flies, in meters per second.")]
    [Min(1f)] [SerializeField] private float arrowSpeed = 25f;
    [Tooltip("Thickness of the arrow for hitting a unit, in meters. It flies to where the target stood when it was shot.")]
    [Min(0.01f)] [SerializeField] private float arrowHitRadius = 0.15f;
    [Tooltip("Off: arrows fly past the shooter's own side. On: the first unit on the arrow's way is hit, friend or foe.")]
    [SerializeField] private bool allowFriendlyFire;

    public bool AutoPauseOnAllyDeath => autoPauseOnAllyDeath;
    public bool AutoPauseOnEnemyDetection => autoPauseOnEnemyDetection;
    public LayerMask SightBlockers => sightBlockers;
    public float EyeHeight => eyeHeight;
    public int MaxQueuedOrders => maxQueuedOrders;
    public Projectile ProjectilePrefab => projectilePrefab;
    public float ArrowSpeed => arrowSpeed;
    public float ArrowHitRadius => arrowHitRadius;
    public bool AllowFriendlyFire => allowFriendlyFire;
}

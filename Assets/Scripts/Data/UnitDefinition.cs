using UnityEngine;

/// <summary>
/// Data describing a type of combat unit (what a "Fantassin" or an "Archer" is).
/// Pure data, editable in the Inspector. Units read it through UnitStats and never write to it.
/// Time-based like the rest of combat: one action costs seconds, and a unit has 'actionTime' seconds per round.
/// </summary>
[CreateAssetMenu(fileName = "UnitDefinition", menuName = "Marchand/Unit Definition")]
public class UnitDefinition : ScriptableObject
{
    [SerializeField] private string displayName = "Unit";

    [Header("Survival")]
    [Min(1)] [SerializeField] private int maxHealth = 40;
    [Tooltip("Subtracted from the damage of every hit that lands.")]
    [Min(0)] [SerializeField] private int armor;
    [Tooltip("Compared to the attacker's roll: the higher, the harder to hit.")]
    [SerializeField] private int defense = 50;

    [Header("Weapon")]
    [Tooltip("Added to the d100 attack roll.")]
    [SerializeField] private int attackBonus = 50;
    [Tooltip("Damage of a hit before the target's armor.")]
    [Min(0)] [SerializeField] private int weaponDamage = 10;
    [Tooltip("Melee weapons: how far the weapon reaches, in meters. 0 for a ranged weapon.")]
    [Min(0f)] [SerializeField] private float reach = 1.8f;
    [Tooltip("Ranged weapons: the weapon cannot hit a target closer than this, in meters. 0 = no dead zone.")]
    [Min(0f)] [SerializeField] private float deadZone;
    [Tooltip("Ranged weapons: farthest target the weapon can hit, in meters. 0 = melee unit.")]
    [Min(0f)] [SerializeField] private float maxRange;
    [Tooltip("How long one attack takes, in seconds. It is spent from the unit's action time, so it also limits how fast a unit can strike.")]
    [Min(0.1f)] [SerializeField] private float attackTime = 2f;

    [Header("Movement and actions")]
    [Tooltip("Walking speed, in meters per second.")]
    [Min(0.1f)] [SerializeField] private float moveSpeed = 1.4f;
    [Tooltip("Action time available per round, in seconds. Every action costs some of it, walking included.")]
    [Min(0.1f)] [SerializeField] private float actionTime = 6f;

    public string DisplayName => displayName;
    public int MaxHealth => maxHealth;
    public int Armor => armor;
    public int Defense => defense;
    public int AttackBonus => attackBonus;
    public int WeaponDamage => weaponDamage;
    public float Reach => reach;
    public float DeadZone => deadZone;
    public float MaxRange => maxRange;
    public float AttackTime => attackTime;
    public float MoveSpeed => moveSpeed;
    public float ActionTime => actionTime;

    /// <summary>True for a ranged unit (it has a maximum range).</summary>
    public bool IsRanged => maxRange > 0f;

    /// <summary>How far the unit walks in one round if it does nothing else: speed times action time.</summary>
    public float MoveDistancePerRound => moveSpeed * actionTime;

    private void OnValidate()
    {
        // A dead zone cannot be larger than the range it belongs to.
        if (maxRange > 0f && deadZone > maxRange)
        {
            deadZone = maxRange;
        }
    }
}

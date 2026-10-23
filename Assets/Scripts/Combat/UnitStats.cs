using UnityEngine;

/// <summary>
/// The combat numbers of one unit. It copies them from its UnitDefinition when it wakes up, so later
/// changes during play (buffs, wounds) never alter the shared asset. Pure data: it does nothing by itself.
/// Which side the unit fights for stays in UnitTeam.
/// </summary>
public class UnitStats : MonoBehaviour
{
    [SerializeField] private UnitDefinition definition;

    public UnitDefinition Definition => definition;
    public string DisplayName { get; private set; }
    public int MaxHealth { get; private set; }
    public int Armor { get; private set; }
    public int Defense { get; private set; }
    public int AttackBonus { get; private set; }
    public int WeaponDamage { get; private set; }
    public float Reach { get; private set; }
    public float DeadZone { get; private set; }
    public float MaxRange { get; private set; }
    public float AttackTime { get; private set; }
    public float MoveSpeed { get; private set; }
    public float ActionTime { get; private set; }

    public bool IsRanged => MaxRange > 0f;
    public float MoveDistancePerRound => MoveSpeed * ActionTime;

    private void Awake()
    {
        if (definition == null)
        {
            Debug.LogError("UnitStats: definition is not assigned.", this);
            enabled = false;
            return;
        }

        DisplayName = definition.DisplayName;
        MaxHealth = definition.MaxHealth;
        Armor = definition.Armor;
        Defense = definition.Defense;
        AttackBonus = definition.AttackBonus;
        WeaponDamage = definition.WeaponDamage;
        Reach = definition.Reach;
        DeadZone = definition.DeadZone;
        MaxRange = definition.MaxRange;
        AttackTime = definition.AttackTime;
        MoveSpeed = definition.MoveSpeed;
        ActionTime = definition.ActionTime;
    }
}

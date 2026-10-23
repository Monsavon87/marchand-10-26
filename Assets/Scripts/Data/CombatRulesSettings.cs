using UnityEngine;

/// <summary>
/// The numbers of the attack rules (the die, the damage floor). Pure data, editable in the Inspector.
/// What a unit brings to a fight (attack bonus, defense, damage, armor) lives in its UnitDefinition.
/// </summary>
[CreateAssetMenu(fileName = "CombatRulesSettings", menuName = "Marchand/Combat Rules Settings")]
public class CombatRulesSettings : ScriptableObject
{
    [Tooltip("Number of faces of the attack die (100 = a d100).")]
    [Min(2)] [SerializeField] private int dieSides = 100;
    [Tooltip("Damage of a hit never goes below this, after the target's armor is subtracted.")]
    [Min(0)] [SerializeField] private int minimumDamage;

    public int DieSides => dieSides;
    public int MinimumDamage => minimumDamage;
}

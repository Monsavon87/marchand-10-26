using System;
using UnityEngine;

/// <summary>One step of the distance penalty of a ranged attack.</summary>
[Serializable]
public struct RangeTier
{
    [Tooltip("This tier covers targets up to this fraction of the weapon's maximum range (0.5 = half of it).")]
    [Range(0.01f, 1f)] public float upToFraction;
    [Tooltip("Added to the attack total (use a negative number: the farther the target, the harder the hit).")]
    public int hitModifier;
}

/// <summary>
/// The numbers of the attack rules (the die, the damage floor, the distance penalty of ranged attacks).
/// Pure data, editable in the Inspector.
/// What a unit brings to a fight (attack bonus, defense, damage, armor) lives in its UnitDefinition.
/// </summary>
[CreateAssetMenu(fileName = "CombatRulesSettings", menuName = "Marchand/Combat Rules Settings")]
public class CombatRulesSettings : ScriptableObject
{
    [Tooltip("Number of faces of the attack die (100 = a d100).")]
    [Min(2)] [SerializeField] private int dieSides = 100;
    [Tooltip("Damage of a hit never goes below this, after the target's armor is subtracted.")]
    [Min(0)] [SerializeField] private int minimumDamage;

    [Header("Ranged attacks")]
    [Tooltip("Distance penalty, from the nearest tier to the farthest. A target beyond the last tier gets the last penalty.")]
    [SerializeField] private RangeTier[] rangeTiers =
    {
        new RangeTier { upToFraction = 0.5f, hitModifier = 0 },
        new RangeTier { upToFraction = 0.75f, hitModifier = -10 },
        new RangeTier { upToFraction = 1f, hitModifier = -25 }
    };

    public int DieSides => dieSides;
    public int MinimumDamage => minimumDamage;

    /// <summary>The modifier to the attack total for a target at this fraction of the weapon's maximum range.</summary>
    public int GetRangeModifier(float rangeFraction)
    {
        if (rangeTiers == null || rangeTiers.Length == 0)
        {
            return 0;
        }

        for (int i = 0; i < rangeTiers.Length; i++)
        {
            if (rangeFraction <= rangeTiers[i].upToFraction)
            {
                return rangeTiers[i].hitModifier;
            }
        }

        return rangeTiers[rangeTiers.Length - 1].hitModifier;
    }
}

using System;
using UnityEngine;

/// <summary>What attacking a given zone changes: a bonus to the attack total and a bonus to the damage.</summary>
[Serializable]
public struct ZoneModifier
{
    [Tooltip("Added to the attack roll total (makes the hit easier).")]
    public int hitBonus;
    [Tooltip("Added to the weapon's damage, before the defender's armor.")]
    public int damageBonus;
}

/// <summary>
/// How the facing of a unit matters: the angles that separate front, flank and back, and what attacking each zone
/// is worth. Pure data, editable in the Inspector.
/// </summary>
[CreateAssetMenu(fileName = "AttackZoneSettings", menuName = "Marchand/Attack Zone Settings")]
public class AttackZoneSettings : ScriptableObject
{
    [Tooltip("Half-width, in degrees, of the front zone around where the unit looks (60 = a 120 degree front).")]
    [Range(1f, 178f)] [SerializeField] private float frontHalfAngle = 60f;
    [Tooltip("Half-width, in degrees, of the back zone around the opposite direction.")]
    [Range(1f, 178f)] [SerializeField] private float backHalfAngle = 60f;

    [Header("Modifiers")]
    [SerializeField] private ZoneModifier front;
    [SerializeField] private ZoneModifier flank = new ZoneModifier { hitBonus = 10, damageBonus = 2 };
    [SerializeField] private ZoneModifier back = new ZoneModifier { hitBonus = 20, damageBonus = 4 };

    public float FrontHalfAngle => frontHalfAngle;
    public float BackHalfAngle => backHalfAngle;

    public ZoneModifier Get(AttackZone zone)
    {
        switch (zone)
        {
            case AttackZone.Front:
                return front;
            case AttackZone.Flank:
                return flank;
            default:
                return back;
        }
    }

    private void OnValidate()
    {
        // Front and back may not overlap: together their half-angles stay within 180 degrees.
        if (frontHalfAngle + backHalfAngle > 180f)
        {
            backHalfAngle = 180f - frontHalfAngle;
        }
    }
}

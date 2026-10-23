using UnityEngine;

/// <summary>
/// The detailed outcome of one attack, for the interface and the combat journal.
/// </summary>
public readonly struct AttackResult
{
    public AttackResult(int roll, int attackBonus, AttackZone zone, ZoneModifier zoneModifier, int threshold, bool hit,
        int weaponDamage, int armor, int damage, int rangeHitModifier = 0, float distance = 0f)
    {
        Roll = roll;
        AttackBonus = attackBonus;
        Zone = zone;
        ZoneHitBonus = zoneModifier.hitBonus;
        ZoneDamageBonus = zoneModifier.damageBonus;
        Threshold = threshold;
        Hit = hit;
        WeaponDamage = weaponDamage;
        Armor = armor;
        Damage = damage;
        RangeHitModifier = rangeHitModifier;
        Distance = distance;
    }

    /// <summary>What the die showed.</summary>
    public int Roll { get; }
    public int AttackBonus { get; }
    /// <summary>Which part of the defender was attacked.</summary>
    public AttackZone Zone { get; }
    /// <summary>Bonus to the total because of the zone (0 from the front).</summary>
    public int ZoneHitBonus { get; }
    /// <summary>Bonus to the damage because of the zone (0 from the front).</summary>
    public int ZoneDamageBonus { get; }
    /// <summary>Modifier to the total because of the distance of a ranged attack (0 in melee, negative when far).</summary>
    public int RangeHitModifier { get; }
    /// <summary>How far the target was, in meters, for a ranged attack. 0 in melee.</summary>
    public float Distance { get; }
    /// <summary>True for an attack made from a distance.</summary>
    public bool IsRanged => Distance > 0f;
    /// <summary>Roll plus attack bonus plus zone bonus plus distance modifier.</summary>
    public int Total => Roll + AttackBonus + ZoneHitBonus + RangeHitModifier;
    /// <summary>What the total had to reach: the target's defense.</summary>
    public int Threshold { get; }
    public bool Hit { get; }
    /// <summary>The weapon's damage, before the zone bonus and the armor.</summary>
    public int WeaponDamage { get; }
    public int Armor { get; }
    /// <summary>Damage really dealt (0 on a miss).</summary>
    public int Damage { get; }
}

/// <summary>
/// How attacks are resolved. Everything that fights goes through this interface, so the rules can be replaced
/// by a fuller system later without touching the actions, the interface or the AI.
/// </summary>
public interface ICombatRules
{
    /// <summary>Rolls the attack die.</summary>
    int RollDie();

    /// <summary>The zone of the defender that an attacker at this position reaches, given where the defender looks.</summary>
    AttackZone DetermineZone(Vector3 attackerPosition, Transform defender);

    /// <summary>What attacking this zone is worth.</summary>
    ZoneModifier GetZoneModifier(AttackZone zone);

    /// <summary>The modifier to the attack total for a ranged attack at this distance (0 near, negative far).</summary>
    int GetRangeModifier(float distance, float maxRange);

    /// <summary>
    /// Chance, in percent, that the attacker hits the defender in this zone. 'rangeModifier' is the distance
    /// modifier of a ranged attack (0 in melee).
    /// </summary>
    int HitChancePercent(UnitStats attacker, UnitStats defender, AttackZone zone, int rangeModifier = 0);

    /// <summary>
    /// Resolves one attack in this zone with the given roll. For a ranged attack, 'distance' is how far the target
    /// was and 'rangeModifier' the distance modifier; both stay 0 in melee.
    /// </summary>
    AttackResult ResolveAttack(UnitStats attacker, UnitStats defender, AttackZone zone, int roll,
        int rangeModifier = 0, float distance = 0f);
}

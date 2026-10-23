using UnityEngine;

/// <summary>
/// The prototype's simplified rules: a die roll plus the attacker's attack bonus (and the bonus of the zone being
/// attacked) must reach the defender's defense to hit; a hit deals the weapon's damage plus the zone's damage
/// bonus, minus the defender's armor, never less than the minimum.
/// Numbers come from CombatRulesSettings, AttackZoneSettings and the units' stats; nothing is hard-coded here.
/// </summary>
public class CombatRules : ICombatRules
{
    private readonly CombatRulesSettings settings;
    private readonly AttackZoneSettings zoneSettings;

    public CombatRules(CombatRulesSettings settings, AttackZoneSettings zoneSettings)
    {
        this.settings = settings;
        this.zoneSettings = zoneSettings;
    }

    public int RollDie()
    {
        return Random.Range(1, settings.DieSides + 1);
    }

    public AttackZone DetermineZone(Vector3 attackerPosition, Transform defender)
    {
        return AttackZones.Determine(attackerPosition, defender.position, defender.forward,
            zoneSettings.FrontHalfAngle, zoneSettings.BackHalfAngle);
    }

    public ZoneModifier GetZoneModifier(AttackZone zone)
    {
        return zoneSettings.Get(zone);
    }

    public int HitChancePercent(UnitStats attacker, UnitStats defender, AttackZone zone)
    {
        // The roll must be at least (defense - attack bonus - zone bonus), and a die starts at 1.
        int lowestHittingRoll = Mathf.Max(1, defender.Defense - attacker.AttackBonus - zoneSettings.Get(zone).hitBonus);
        int hittingRolls = Mathf.Clamp(settings.DieSides - lowestHittingRoll + 1, 0, settings.DieSides);
        return Mathf.RoundToInt(hittingRolls * 100f / settings.DieSides);
    }

    public AttackResult ResolveAttack(UnitStats attacker, UnitStats defender, AttackZone zone, int roll)
    {
        ZoneModifier modifier = zoneSettings.Get(zone);
        bool hit = roll + attacker.AttackBonus + modifier.hitBonus >= defender.Defense;
        int damage = hit
            ? Mathf.Max(settings.MinimumDamage, attacker.WeaponDamage + modifier.damageBonus - defender.Armor)
            : 0;
        return new AttackResult(roll, attacker.AttackBonus, zone, modifier, defender.Defense, hit,
            attacker.WeaponDamage, defender.Armor, damage);
    }
}

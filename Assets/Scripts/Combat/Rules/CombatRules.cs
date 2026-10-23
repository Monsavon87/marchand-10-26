using UnityEngine;

/// <summary>
/// The prototype's simplified rules: a die roll plus the attacker's attack bonus must reach the defender's defense
/// to hit; a hit deals the weapon's damage minus the defender's armor, never less than the minimum.
/// Numbers come from CombatRulesSettings and from the units' stats; nothing is hard-coded here.
/// </summary>
public class CombatRules : ICombatRules
{
    private readonly CombatRulesSettings settings;

    public CombatRules(CombatRulesSettings settings)
    {
        this.settings = settings;
    }

    public int RollDie()
    {
        return Random.Range(1, settings.DieSides + 1);
    }

    public int HitChancePercent(UnitStats attacker, UnitStats defender)
    {
        // The roll must be at least (defense - attack bonus), and a die starts at 1.
        int lowestHittingRoll = Mathf.Max(1, defender.Defense - attacker.AttackBonus);
        int hittingRolls = Mathf.Clamp(settings.DieSides - lowestHittingRoll + 1, 0, settings.DieSides);
        return Mathf.RoundToInt(hittingRolls * 100f / settings.DieSides);
    }

    public AttackResult ResolveAttack(UnitStats attacker, UnitStats defender, int roll)
    {
        bool hit = roll + attacker.AttackBonus >= defender.Defense;
        int rawDamage = hit ? attacker.WeaponDamage : 0;
        int damage = hit ? Mathf.Max(settings.MinimumDamage, rawDamage - defender.Armor) : 0;
        return new AttackResult(roll, attacker.AttackBonus, defender.Defense, hit, rawDamage, defender.Armor, damage);
    }
}

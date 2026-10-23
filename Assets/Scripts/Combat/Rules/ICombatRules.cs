/// <summary>
/// The detailed outcome of one attack, for the interface and the combat journal.
/// </summary>
public readonly struct AttackResult
{
    public AttackResult(int roll, int attackBonus, int threshold, bool hit, int rawDamage, int armor, int damage)
    {
        Roll = roll;
        AttackBonus = attackBonus;
        Threshold = threshold;
        Hit = hit;
        RawDamage = rawDamage;
        Armor = armor;
        Damage = damage;
    }

    /// <summary>What the die showed.</summary>
    public int Roll { get; }
    public int AttackBonus { get; }
    /// <summary>Roll plus attack bonus.</summary>
    public int Total => Roll + AttackBonus;
    /// <summary>What the total had to reach: the target's defense.</summary>
    public int Threshold { get; }
    public bool Hit { get; }
    /// <summary>Damage of the weapon before armor (0 on a miss).</summary>
    public int RawDamage { get; }
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

    /// <summary>Chance, in percent, that the attacker hits the defender.</summary>
    int HitChancePercent(UnitStats attacker, UnitStats defender);

    /// <summary>Resolves one attack with the given roll.</summary>
    AttackResult ResolveAttack(UnitStats attacker, UnitStats defender, int roll);
}

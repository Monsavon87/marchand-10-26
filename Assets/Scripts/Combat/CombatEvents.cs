using System;
using UnityEngine;

/// <summary>Everything the interface needs to show one resolved attack.</summary>
public readonly struct AttackReport
{
    public AttackReport(string attackerName, string defenderName, Team attackerTeam, AttackResult result,
        Vector3 defenderPosition, int defenderHealth, int defenderMaxHealth, bool defenderKilled)
    {
        AttackerName = attackerName;
        DefenderName = defenderName;
        AttackerTeam = attackerTeam;
        Result = result;
        DefenderPosition = defenderPosition;
        DefenderHealth = defenderHealth;
        DefenderMaxHealth = defenderMaxHealth;
        DefenderKilled = defenderKilled;
    }

    public string AttackerName { get; }
    public string DefenderName { get; }
    public Team AttackerTeam { get; }
    public AttackResult Result { get; }
    public Vector3 DefenderPosition { get; }
    /// <summary>The defender's hit points after the attack.</summary>
    public int DefenderHealth { get; }
    public int DefenderMaxHealth { get; }
    public bool DefenderKilled { get; }
}

/// <summary>
/// Combat happenings that many unrelated listeners care about (floating texts, the journal, later sounds...).
/// Whoever resolves an attack raises it; listeners subscribe while enabled and unsubscribe when disabled.
/// </summary>
public static class CombatEvents
{
    public static event Action<AttackReport> AttackResolved = delegate { };

    public static void RaiseAttackResolved(AttackReport report)
    {
        AttackResolved(report);
    }
}

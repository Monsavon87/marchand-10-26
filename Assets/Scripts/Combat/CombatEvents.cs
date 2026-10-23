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

    /// <summary>A line for the combat journal that is not an attack (a shot that cannot be taken, an arrow lost...).</summary>
    public static event Action<string> Note = delegate { };

    /// <summary>An order was refused or could not be carried out: (side of the unit, unit name, reason).</summary>
    public static event Action<Team, string, string> OrderRefused = delegate { };

    /// <summary>A unit has just noticed an enemy unit for the first time: (the one who noticed, the one noticed).</summary>
    public static event Action<UnitTeam, UnitTeam> UnitDetected = delegate { };

    public static void RaiseUnitDetected(UnitTeam detector, UnitTeam detected)
    {
        UnitDetected(detector, detected);
    }

    public static void RaiseOrderRefused(Team side, string unitName, string reason)
    {
        OrderRefused(side, unitName, reason);
    }

    public static void RaiseAttackResolved(AttackReport report)
    {
        AttackResolved(report);
    }

    public static void RaiseNote(string text)
    {
        Note(text);
    }
}

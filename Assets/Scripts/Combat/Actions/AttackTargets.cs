using UnityEngine;

/// <summary>What the attack actions (melee and ranged) both need to know about a target and its name.</summary>
public static class AttackTargets
{
    /// <summary>Reads the target's parts and checks that an attacker of this side can fight it.</summary>
    public static bool TryResolve(Transform target, Team attackerTeam, out Health health, out UnitStats stats,
        out string reason)
    {
        health = null;
        stats = null;
        if (target == null)
        {
            reason = "aucune cible";
            return false;
        }

        health = target.GetComponentInParent<Health>();
        stats = target.GetComponentInParent<UnitStats>();
        UnitTeam targetTeam = target.GetComponentInParent<UnitTeam>();
        if (health == null || stats == null || targetTeam == null)
        {
            reason = "cette cible ne peut pas être attaquée";
            return false;
        }

        if (health.IsDead)
        {
            reason = "la cible est déjà morte";
            return false;
        }

        if (targetTeam.Team == attackerTeam)
        {
            reason = "la cible est du même camp";
            return false;
        }

        reason = null;
        return true;
    }

    /// <summary>The unit's name as written in the journal.</summary>
    public static string Describe(UnitStats unit)
    {
        return unit.name + " (" + unit.DisplayName + ")";
    }
}

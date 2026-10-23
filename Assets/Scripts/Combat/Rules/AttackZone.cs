using UnityEngine;

/// <summary>Which part of the defender an attack lands on, depending on where the attacker stands.</summary>
public enum AttackZone
{
    Front,
    Flank,
    Back
}

/// <summary>
/// Works out the attack zone from positions and the defender's facing. Pure geometry, no game data inside:
/// the angles are parameters (they come from AttackZoneSettings).
/// </summary>
public static class AttackZones
{
    /// <summary>
    /// The zone of the defender that an attacker standing at 'attackerPosition' reaches.
    /// Front: within 'frontHalfAngle' degrees of where the defender looks. Back: within 'backHalfAngle' degrees of
    /// the opposite direction. Flank: everything in between. Heights are ignored.
    /// </summary>
    public static AttackZone Determine(Vector3 attackerPosition, Vector3 defenderPosition, Vector3 defenderForward,
        float frontHalfAngle, float backHalfAngle)
    {
        Vector3 toAttacker = attackerPosition - defenderPosition;
        toAttacker.y = 0f;
        defenderForward.y = 0f;
        if (toAttacker.sqrMagnitude < 0.0001f || defenderForward.sqrMagnitude < 0.0001f)
        {
            return AttackZone.Front;
        }

        float angle = Vector3.Angle(defenderForward, toAttacker);
        if (angle <= frontHalfAngle)
        {
            return AttackZone.Front;
        }

        return angle >= 180f - backHalfAngle ? AttackZone.Back : AttackZone.Flank;
    }

    /// <summary>The French name of a zone, for the interface.</summary>
    public static string Name(AttackZone zone)
    {
        switch (zone)
        {
            case AttackZone.Front:
                return "face";
            case AttackZone.Flank:
                return "flanc";
            default:
                return "dos";
        }
    }
}

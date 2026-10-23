using System;
using UnityEngine;

/// <summary>
/// One member of a group (the player's caravan or an enemy band on the campaign map): what kind of unit it is and
/// how many hit points it has left. It is what a combat unit is made from, and what comes back from the fight.
/// </summary>
[Serializable]
public class PartyMember
{
    [Tooltip("Name of the unit in combat.")]
    public string name = "Unité";
    public UnitDefinition definition;
    [Tooltip("Hit points left. 0 = full health (the definition's maximum).")]
    [Min(0)] public int currentHealth;
    [Tooltip("Enemies only: how this unit fights. Empty = the arena's default profile.")]
    public AIProfile aiProfile;

    /// <summary>The hit points the unit starts the fight with.</summary>
    public int StartingHealth
    {
        get
        {
            if (definition == null)
            {
                return 1;
            }

            return currentHealth > 0 ? Mathf.Min(currentHealth, definition.MaxHealth) : definition.MaxHealth;
        }
    }

    public PartyMember Copy()
    {
        return (PartyMember)MemberwiseClone();
    }
}

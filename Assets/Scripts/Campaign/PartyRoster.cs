using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The group behind a pawn of the campaign map: the units that fight when an encounter turns into a combat, with
/// their hit points, and for an enemy band the loot it leaves (in U.V.). The caravan's roster also keeps the total
/// loot gathered, a statistic: the money itself is the Wallet's business.
/// Pure data plus the update after a fight; it never moves anything.
/// </summary>
public class PartyRoster : MonoBehaviour
{
    [SerializeField] private List<PartyMember> members = new List<PartyMember>();
    [Tooltip("Enemy bands: what defeating them is worth.")]
    [Min(0)] [SerializeField] private int loot;

    public IReadOnlyList<PartyMember> Members => members;
    public int Loot => loot;
    /// <summary>Total loot gathered so far (the caravan's), in U.V., spent or not. A statistic only.</summary>
    public int Gathered { get; private set; }

    /// <summary>
    /// After a fight: each member gets its hit points back from 'healthAfter' (same order as the roster); the
    /// members at 0 died and leave the group.
    /// </summary>
    public void ApplyHealth(int[] healthAfter)
    {
        if (healthAfter == null || healthAfter.Length != members.Count)
        {
            Debug.LogError("PartyRoster: the fight result does not match the roster of " + name + ".", this);
            return;
        }

        for (int i = members.Count - 1; i >= 0; i--)
        {
            if (healthAfter[i] <= 0)
            {
                members.RemoveAt(i);
            }
            else
            {
                members[i].currentHealth = healthAfter[i];
            }
        }
    }

    /// <summary>Every member gets all its hit points back (a rest in town).</summary>
    public void HealAll()
    {
        for (int i = 0; i < members.Count; i++)
        {
            members[i].currentHealth = 0;
        }
    }

    /// <summary>Counts loot in the statistic of the total gathered. It does not pay anything: that is the Wallet's job.</summary>
    public void RecordLoot(int amount)
    {
        Gathered += Mathf.Max(0, amount);
    }

    /// <summary>A new member joins the group (a recruit).</summary>
    public void AddMember(PartyMember member)
    {
        members.Add(member);
    }

    public bool HasMemberNamed(string memberName)
    {
        for (int i = 0; i < members.Count; i++)
        {
            if (members[i].name == memberName)
            {
                return true;
            }
        }

        return false;
    }
}

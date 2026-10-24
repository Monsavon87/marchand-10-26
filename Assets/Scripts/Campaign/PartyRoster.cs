using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The group behind a pawn of the campaign map: the units that fight when an encounter turns into a combat, with
/// their hit points, and for an enemy band the loot it leaves: a loot table (what it can carry) and, once the band
/// is created, the loot rolled from it. The caravan's roster also keeps the total value gathered, a statistic: the
/// money itself is the Wallet's business.
/// Pure data plus the update after a fight; it never moves anything.
/// </summary>
public class PartyRoster : MonoBehaviour
{
    [SerializeField] private List<PartyMember> members = new List<PartyMember>();
    [Tooltip("Enemy bands: what the band can carry. Rolled once when the band is created (see BandLootSetup).")]
    [SerializeField] private LootTable lootTable;

    public IReadOnlyList<PartyMember> Members => members;
    public LootTable LootTable => lootTable;
    /// <summary>The loot this band carries, rolled when it was created (empty until then, and for the caravan).</summary>
    public LootResult Loot { get; private set; } = LootResult.Empty;
    /// <summary>Total value of the loot gathered so far (the caravan's): U.V. plus goods at base price. A statistic only.</summary>
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

    /// <summary>True when at least one member has lost hit points.</summary>
    public bool HasWounded
    {
        get
        {
            for (int i = 0; i < members.Count; i++)
            {
                if (members[i].IsWounded)
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    /// One day of natural healing for every member (see HealingRules). Returns the hit points given back in all.
    /// </summary>
    public int RegenerateDay(HealingSettings settings)
    {
        int given = 0;
        for (int i = 0; i < members.Count; i++)
        {
            PartyMember member = members[i];
            int before = member.StartingHealth;
            int after = HealingRules.NextHealth(before, member.MaxHealth, settings.RegenPercentPerDay, settings.RegenCapPercent);
            if (after != before)
            {
                member.SetHealth(after);
                given += after - before;
            }
        }

        return given;
    }

    /// <summary>Gives the band the loot rolled from its table (done once, when the band is created).</summary>
    public void SetLoot(LootResult loot)
    {
        Loot = loot ?? LootResult.Empty;
    }

    /// <summary>Counts loot in the statistic of the total gathered. It does not pay anything: that is the Wallet's job.</summary>
    public void RecordLoot(int amount)
    {
        Gathered += Mathf.Max(0, amount);
    }

    /// <summary>The group becomes exactly these members (copies): a band that came back with its original composition.</summary>
    public void ReplaceMembers(IReadOnlyList<PartyMember> newMembers)
    {
        members.Clear();
        for (int i = 0; i < newMembers.Count; i++)
        {
            members.Add(newMembers[i].Copy());
        }
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

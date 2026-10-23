using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The group behind a pawn of the campaign map: the units that fight when an encounter turns into a combat, with
/// their hit points, and for an enemy band the loot it leaves. The caravan's roster also keeps the loot gathered.
/// Pure data plus the update after a fight; it never moves anything.
/// </summary>
public class PartyRoster : MonoBehaviour
{
    [SerializeField] private List<PartyMember> members = new List<PartyMember>();
    [Tooltip("Enemy bands: what defeating them is worth.")]
    [Min(0)] [SerializeField] private int loot;

    public IReadOnlyList<PartyMember> Members => members;
    public int Loot => loot;
    /// <summary>Loot gathered so far (the caravan's).</summary>
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

    public void AddLoot(int amount)
    {
        Gathered += Mathf.Max(0, amount);
    }
}

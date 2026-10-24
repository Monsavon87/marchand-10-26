using System;
using UnityEngine;

/// <summary>
/// Gives every enemy band of the map its loot when the scene starts, and again each time a band comes back (see
/// BandRespawner): the band's loot table is rolled with dice seeded from this component's seed and the band's name (and
/// how many times it has come back), so each band always gets the same loot for the same seed whatever the order the
/// bands are found in. Seed 0 means a different loot each game. A band with no table is a setup mistake and is reported.
/// </summary>
public class BandLootSetup : MonoBehaviour
{
    [Tooltip("0 = a different loot each game; any other number = always the same loot (tests).")]
    [SerializeField] private int seed;

    private int baseSeed;

    private void Start()
    {
        baseSeed = seed != 0 ? seed : Environment.TickCount;
        var units = UnitTeam.All;
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i].Team == Team.Enemy && units[i].TryGetComponent(out PartyRoster roster))
            {
                RollFor(roster, 0);
            }
        }
    }

    /// <summary>
    /// Rolls the loot of a band: the first time at the start of the scene (round 0), and again each time the band
    /// comes back ('round' is the number of times it has come back, so each return gives another, repeatable, loot).
    /// </summary>
    public void RollFor(PartyRoster roster, int round)
    {
        if (roster.LootTable == null)
        {
            Debug.LogError("BandLootSetup: the band '" + roster.name + "' has no loot table: it will leave nothing.", roster);
            return;
        }

        string key = round == 0 ? roster.name : roster.name + "#" + round;
        roster.SetLoot(LootRoller.Roll(roster.LootTable, new System.Random(Mix(baseSeed, key))));
    }

    // A seed from the base seed and a name (FNV-1a), the same on every run and every machine.
    private static int Mix(int baseSeed, string text)
    {
        unchecked
        {
            uint hash = 2166136261u ^ (uint)baseSeed;
            for (int i = 0; i < text.Length; i++)
            {
                hash = (hash ^ text[i]) * 16777619u;
            }

            return (int)hash;
        }
    }
}

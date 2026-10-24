using System;
using UnityEngine;

/// <summary>
/// Gives every enemy band of the map its loot, once, when the scene starts: the band's loot table is rolled with dice
/// seeded from this component's seed and the band's name, so each band always gets the same loot for the same seed
/// whatever the order the bands are found in. Seed 0 means a different loot each game. A band with no table is a
/// setup mistake and is reported.
/// </summary>
public class BandLootSetup : MonoBehaviour
{
    [Tooltip("0 = a different loot each game; any other number = always the same loot (tests).")]
    [SerializeField] private int seed;

    private void Start()
    {
        int baseSeed = seed != 0 ? seed : Environment.TickCount;
        var units = UnitTeam.All;
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i].Team != Team.Enemy || !units[i].TryGetComponent(out PartyRoster roster))
            {
                continue;
            }

            if (roster.LootTable == null)
            {
                Debug.LogError("BandLootSetup: the band '" + roster.name + "' has no loot table: it will leave nothing.", roster);
                continue;
            }

            roster.SetLoot(LootRoller.Roll(roster.LootTable, new System.Random(Mix(baseSeed, roster.name))));
        }
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

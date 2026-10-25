using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// What real fights came to, measured on the combat test bench (the arena's combat with both sides played by the
/// combat AI): for each matchup of a caravan's composition against a band's, the share of fights won, the share of
/// its hit points the caravan lost when it won, and the units it lost. A composition is a count of each kind of unit
/// ("Def_Fantassin x2, Def_Archer x1"). The economy simulator reads it to decide whether a fight is worth it and what
/// it costs. Pure data, written by the bench.
/// </summary>
[CreateAssetMenu(fileName = "CombatOdds", menuName = "Marchand/Economy/Combat Odds Table")]
public class CombatOddsTable : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Tooltip("The caravan's units, as a composition key (see CombatOddsTable.KeyOf).")]
        public string caravan;
        [Tooltip("The band's units, as a composition key.")]
        public string band;
        public int caravanUnits;
        public int fights;
        public int wins;
        [Tooltip("Mean share of its hit points the caravan lost in a won fight (0 to 1).")]
        public float meanHealthLost;
        [Tooltip("Largest share lost in a won fight (0 to 1).")]
        public float maxHealthLost;
        [Tooltip("Units lost per won fight, on average.")]
        public float deathsPerWin;

        public float WinShare => fights > 0 ? wins / (float)fights : 0f;
    }

    [TextArea] [SerializeField] private string source = "";
    [SerializeField] private List<Entry> entries = new List<Entry>();

    public IReadOnlyList<Entry> Entries => entries;
    public string Source => source;

    /// <summary>The composition key of a list of units: each kind with its count, sorted by name ("Def_Archer x1, Def_Fantassin x2").</summary>
    public static string KeyOf(IEnumerable<UnitDefinition> units)
    {
        var counts = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (UnitDefinition unit in units)
        {
            if (unit == null)
            {
                continue;
            }

            counts.TryGetValue(unit.name, out int count);
            counts[unit.name] = count + 1;
        }

        var parts = new List<string>();
        foreach (KeyValuePair<string, int> pair in counts)
        {
            parts.Add(pair.Key + " x" + pair.Value);
        }

        return string.Join(", ", parts);
    }

    /// <summary>
    /// The measured matchup: the exact caravan composition against this band, or else the measured caravan with the
    /// same number of units (and then the nearest number). Null when the band was never measured.
    /// </summary>
    public Entry Find(string caravanKey, int caravanUnits, string bandKey)
    {
        Entry best = null;
        int bestGap = int.MaxValue;
        for (int i = 0; i < entries.Count; i++)
        {
            Entry entry = entries[i];
            if (entry.band != bandKey)
            {
                continue;
            }

            if (entry.caravan == caravanKey)
            {
                return entry;
            }

            int gap = Math.Abs(entry.caravanUnits - caravanUnits);
            if (gap < bestGap)
            {
                bestGap = gap;
                best = entry;
            }
        }

        return best;
    }

    /// <summary>Replaces the table (the bench writes it).</summary>
    public void Set(IEnumerable<Entry> measured, string description)
    {
        entries.Clear();
        entries.AddRange(measured);
        source = description;
    }
}

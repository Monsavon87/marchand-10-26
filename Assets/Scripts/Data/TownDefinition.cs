using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>What a town trades in ONE good: its stock at the start, the stock it tends to, and what it makes and uses each day.</summary>
[Serializable]
public class GoodStockProfile
{
    public GoodDefinition good;
    [Tooltip("Units in stock when the game starts.")]
    [Min(0)] public int initialStock = 10;
    [Tooltip("The stock where the price is the base price. More than 0.")]
    [Min(1)] public int targetStock = 10;
    [Tooltip("Units the town makes each day.")]
    [Min(0)] public int productionPerDay;
    [Tooltip("Units the town uses each day.")]
    [Min(0)] public int consumptionPerDay;
}

/// <summary>
/// The trading profile of a town (agricultural, port, mining...): one entry per good it trades; a good without an
/// entry is not traded there. Pure data, editable in the Inspector. All amounts are whole numbers, in units of
/// measure of the good.
/// </summary>
[CreateAssetMenu(fileName = "TownDefinition", menuName = "Marchand/Economy/Town Definition")]
public class TownDefinition : ScriptableObject
{
    [Tooltip("What kind of town it is (Agricole, Portuaire...), for the debug view.")]
    [SerializeField] private string profileName = "Profil";
    [SerializeField] private List<GoodStockProfile> entries = new List<GoodStockProfile>();

    public string ProfileName => profileName;
    public IReadOnlyList<GoodStockProfile> Entries => entries;

    /// <summary>Finds the entry of a good. False (and null) when the town does not trade it.</summary>
    public bool TryGetEntry(GoodDefinition good, out GoodStockProfile entry)
    {
        entry = null;
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && entries[i].good == good && good != null)
            {
                entry = entries[i];
                return true;
            }
        }

        return false;
    }

    /// <summary>Writes a warning for each invalid entry, and returns how many there were.</summary>
    public int Validate()
    {
        int problems = 0;
        var seen = new HashSet<GoodDefinition>();
        for (int i = 0; i < entries.Count; i++)
        {
            GoodStockProfile entry = entries[i];
            if (entry == null || entry.good == null)
            {
                Debug.LogWarning("TownDefinition '" + name + "': entry " + i + " has no good.", this);
                problems++;
                continue;
            }

            if (!seen.Add(entry.good))
            {
                Debug.LogWarning("TownDefinition '" + name + "': the good '" + entry.good.Id + "' is traded twice.", this);
                problems++;
            }

            if (entry.initialStock < 0 || entry.productionPerDay < 0 || entry.consumptionPerDay < 0)
            {
                Debug.LogWarning("TownDefinition '" + name + "': '" + entry.good.Id + "' has a negative amount.", this);
                problems++;
            }

            if (entry.targetStock <= 0)
            {
                Debug.LogWarning("TownDefinition '" + name + "': the target stock of '" + entry.good.Id + "' must be more than 0.", this);
                problems++;
            }
        }

        return problems;
    }

    private void OnValidate()
    {
        Validate();
    }
}

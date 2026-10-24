using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LootEntry
{
    public GoodDefinition good;
    [Tooltip("Relative weight of this good in the split of the loot's value. More than 0.")]
    [Min(1)] public int weight = 10;
}

/// <summary>
/// What a band can carry: a total value between a minimum and a maximum (in U.V., goods counted at their base price),
/// split at random between the U.V. and the goods listed here, each according to its weight. Rolled once, when the
/// band is created (see LootRoller). Pure data.
/// </summary>
[CreateAssetMenu(fileName = "LootTable", menuName = "Marchand/Economy/Loot Table")]
public class LootTable : ScriptableObject
{
    [Tooltip("Least value of the loot, in U.V. at base price.")]
    [Min(0)] [SerializeField] private int minValue = 1000;
    [Tooltip("Greatest value of the loot, in U.V. at base price.")]
    [Min(0)] [SerializeField] private int maxValue = 3000;
    [Tooltip("Weight of the money in the split. More than 0: every band gives some U.V.")]
    [Min(1)] [SerializeField] private int uvWeight = 30;
    [SerializeField] private List<LootEntry> goods = new List<LootEntry>();

    public int MinValue => minValue;
    public int MaxValue => maxValue;
    public int UvWeight => uvWeight;
    public IReadOnlyList<LootEntry> Goods => goods;

    /// <summary>Sum of the weights of the U.V. and of every good.</summary>
    public int TotalWeight
    {
        get
        {
            int total = uvWeight;
            for (int i = 0; i < goods.Count; i++)
            {
                total += goods[i] != null ? goods[i].weight : 0;
            }

            return total;
        }
    }

    /// <summary>Writes a warning for each problem found and returns how many there were.</summary>
    public int Validate()
    {
        int problems = 0;
        if (minValue > maxValue)
        {
            Debug.LogWarning("LootTable '" + name + "': the minimum value is above the maximum.", this);
            problems++;
        }

        if (uvWeight <= 0)
        {
            Debug.LogWarning("LootTable '" + name + "': the weight of the U.V. must be more than 0.", this);
            problems++;
        }

        var seen = new HashSet<GoodDefinition>();
        for (int i = 0; i < goods.Count; i++)
        {
            LootEntry entry = goods[i];
            if (entry == null || entry.good == null)
            {
                Debug.LogWarning("LootTable '" + name + "': entry " + i + " has no good.", this);
                problems++;
                continue;
            }

            if (!seen.Add(entry.good))
            {
                Debug.LogWarning("LootTable '" + name + "': the good '" + entry.good.Id + "' is listed twice.", this);
                problems++;
            }

            if (entry.weight <= 0)
            {
                Debug.LogWarning("LootTable '" + name + "': the weight of '" + entry.good.Id + "' must be more than 0.", this);
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

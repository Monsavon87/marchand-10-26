using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The list of every good in the game, with a lookup by identifier. Pure data plus the lookup. It checks itself
/// when it loads and when it is edited: it warns in the console about an empty entry, a duplicated identifier or
/// an invalid value (price, mass or volume that is not more than 0), without ever stopping the game.
/// </summary>
[CreateAssetMenu(fileName = "GoodCatalog", menuName = "Marchand/Economy/Good Catalog")]
public class GoodCatalog : ScriptableObject
{
    [SerializeField] private List<GoodDefinition> goods = new List<GoodDefinition>();

    private Dictionary<string, GoodDefinition> byId;

    public IReadOnlyList<GoodDefinition> Goods => goods;

    /// <summary>Finds a good by its identifier. False (and null) when there is none.</summary>
    public bool TryGetGood(string id, out GoodDefinition good)
    {
        good = null;
        if (string.IsNullOrEmpty(id))
        {
            return false;
        }

        if (byId == null)
        {
            BuildIndex();
        }

        return byId.TryGetValue(id, out good);
    }

    /// <summary>
    /// Checks the whole catalog and writes a warning for each problem found. Returns how many there were
    /// (0 = the catalog is sound).
    /// </summary>
    public int Validate()
    {
        int problems = 0;
        var seen = new HashSet<string>();
        for (int i = 0; i < goods.Count; i++)
        {
            GoodDefinition good = goods[i];
            if (good == null)
            {
                Debug.LogWarning("GoodCatalog: entry " + i + " is empty.", this);
                problems++;
                continue;
            }

            problems += good.Validate();
            if (!string.IsNullOrWhiteSpace(good.Id) && !seen.Add(good.Id))
            {
                Debug.LogWarning("GoodCatalog: the identifier '" + good.Id + "' is used by more than one good.", this);
                problems++;
            }
        }

        return problems;
    }

    private void BuildIndex()
    {
        byId = new Dictionary<string, GoodDefinition>();
        for (int i = 0; i < goods.Count; i++)
        {
            GoodDefinition good = goods[i];
            // With a duplicated identifier the first good wins (Validate has already warned about it).
            if (good != null && !string.IsNullOrWhiteSpace(good.Id) && !byId.ContainsKey(good.Id))
            {
                byId.Add(good.Id, good);
            }
        }
    }

    private void OnEnable()
    {
        byId = null;
        Validate();
    }

    private void OnValidate()
    {
        byId = null;
        Validate();
    }
}

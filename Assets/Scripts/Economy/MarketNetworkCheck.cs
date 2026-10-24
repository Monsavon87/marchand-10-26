using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Checks that the markets of a map work together: every good of the catalog is made (more than it is used) in at
/// least one town and used (more than it is made) in at least one other, and no two towns share the same profile.
/// Writes a warning for each problem found and returns how many there were.
/// </summary>
public static class MarketNetworkCheck
{
    public static int Validate(IReadOnlyList<Market> markets, GoodCatalog catalog)
    {
        int problems = 0;
        for (int i = 0; i < markets.Count; i++)
        {
            for (int j = i + 1; j < markets.Count; j++)
            {
                if (markets[i].Definition == markets[j].Definition)
                {
                    Debug.LogWarning("Market network: '" + markets[i].TownName + "' and '" + markets[j].TownName
                        + "' share the same profile '" + markets[i].Definition.name + "'.", markets[j]);
                    problems++;
                }
            }
        }

        IReadOnlyList<GoodDefinition> goods = catalog.Goods;
        for (int g = 0; g < goods.Count; g++)
        {
            GoodDefinition good = goods[g];
            if (good == null)
            {
                continue;
            }

            int makers = 0;
            int users = 0;
            for (int m = 0; m < markets.Count; m++)
            {
                if (markets[m].Definition.TryGetEntry(good, out GoodStockProfile entry))
                {
                    int net = entry.productionPerDay - entry.consumptionPerDay;
                    if (net > 0)
                    {
                        makers++;
                    }
                    else if (net < 0)
                    {
                        users++;
                    }
                }
            }

            if (makers == 0 || users == 0)
            {
                Debug.LogWarning("Market network: the good '" + good.Id + "' has " + makers + " town(s) that make more than they use and "
                    + users + " that use more than they make: it needs at least one of each.");
                problems++;
            }
        }

        return problems;
    }
}

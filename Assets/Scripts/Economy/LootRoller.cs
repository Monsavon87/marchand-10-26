using System;
using System.Collections.Generic;

/// <summary>
/// Rolls a loot table: draws the total value between the table's minimum and maximum, splits it at random between
/// the U.V. and the goods (each part is the good's weight times a random draw, as a share of the total), and turns the
/// part of each good into whole units with a random rounding: 2.3 units give 2, or 3 with a chance of 0.3, so that over
/// many draws each good gets the share its weight promises even when it is dear. The U.V. get everything the goods do
/// not take, so the value of the result is always exactly the value drawn. It takes the dice as a parameter: the same
/// dice state gives the same loot, which lets the game seed it and the economy simulator reproduce a run. It touches
/// no scene.
/// </summary>
public static class LootRoller
{
    public static LootResult Roll(LootTable table, Random dice)
    {
        if (table == null || dice == null)
        {
            throw new ArgumentNullException(table == null ? nameof(table) : nameof(dice));
        }

        int low = Math.Max(0, table.MinValue);
        int high = Math.Max(low, table.MaxValue);
        int value = low == high ? low : dice.Next(low, high + 1);

        IReadOnlyList<LootEntry> entries = table.Goods;
        var parts = new double[entries.Count];
        double weightSum = Math.Max(1, table.UvWeight) * Exponential(dice);
        for (int i = 0; i < entries.Count; i++)
        {
            double part = entries[i] != null && entries[i].good != null ? Math.Max(1, entries[i].weight) * Exponential(dice) : 0.0;
            parts[i] = part;
            weightSum += part;
        }

        var goods = new List<GoodStack>();
        long goodsValue = 0;
        for (int i = 0; i < entries.Count; i++)
        {
            LootEntry entry = entries[i];
            if (entry == null || entry.good == null)
            {
                continue;
            }

            int price = Math.Max(1, entry.good.BasePrice);
            double units = value * parts[i] / weightSum / price;
            int quantity = (int)Math.Floor(units);
            if (dice.NextDouble() < units - quantity)
            {
                quantity++;
            }

            if (quantity > 0)
            {
                goods.Add(new GoodStack(entry.good, quantity));
                goodsValue += (long)quantity * price;
            }
        }

        // The rounding can make the goods worth more than the value drawn: take units back, from a good picked at
        // random each time (so no good is always the one that pays), until the money is not negative.
        while (goodsValue > value && goods.Count > 0)
        {
            int index = dice.Next(goods.Count);
            GoodStack picked = goods[index];
            goodsValue -= picked.Good.BasePrice;
            if (picked.Quantity > 1)
            {
                goods[index] = new GoodStack(picked.Good, picked.Quantity - 1);
            }
            else
            {
                goods.RemoveAt(index);
            }
        }

        return new LootResult((int)(value - goodsValue), goods);
    }

    // An exponentially distributed positive number (mean 1): split by such weights, the parts follow a Dirichlet law,
    // which is the natural "random split in proportion to the weights".
    private static double Exponential(Random dice)
    {
        return -Math.Log(1.0 - dice.NextDouble());
    }
}

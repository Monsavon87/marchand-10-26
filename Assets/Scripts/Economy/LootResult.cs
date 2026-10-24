using System.Collections.Generic;

/// <summary>
/// What a band carries once its loot table has been rolled: an amount of U.V. and some goods. Its value is the sum
/// of the U.V. and of the goods at their base price, which is the value the table drew. Plain data, never changed.
/// </summary>
public class LootResult
{
    public static readonly LootResult Empty = new LootResult(0, new List<GoodStack>());

    private readonly List<GoodStack> goods;

    public LootResult(int uv, List<GoodStack> goods)
    {
        Uv = uv;
        this.goods = goods;
    }

    /// <summary>The money part, in U.V.</summary>
    public int Uv { get; }

    public IReadOnlyList<GoodStack> Goods => goods;

    /// <summary>U.V. plus the goods at their base price.</summary>
    public int Value
    {
        get
        {
            long total = Uv;
            for (int i = 0; i < goods.Count; i++)
            {
                total += goods[i].BaseValue;
            }

            return (int)System.Math.Min(total, int.MaxValue);
        }
    }
}

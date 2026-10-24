using System.Collections.Generic;
using System.Text;

/// <summary>The sentences that tell the player what a loot is made of. No rule here, only wording.</summary>
public static class LootMessages
{
    /// <summary>"Sel x3, Cuir x1" (empty when there is nothing).</summary>
    public static string Goods(IReadOnlyList<GoodStack> goods)
    {
        var text = new StringBuilder();
        for (int i = 0; i < goods.Count; i++)
        {
            if (text.Length > 0)
            {
                text.Append(", ");
            }

            text.Append(goods[i].Good.DisplayName).Append(" x").Append(goods[i].Quantity);
        }

        return text.ToString();
    }

    /// <summary>The whole report of a victory: money, goods, and what the cargo could not take.</summary>
    public static string Report(int uv, IReadOnlyList<GoodStack> taken, IReadOnlyList<GoodStack> lost)
    {
        var text = new StringBuilder();
        if (uv > 0)
        {
            text.Append("Butin : ").Append(MoneyFormat.WithUnit(uv));
        }

        if (taken.Count > 0)
        {
            text.Append(text.Length > 0 ? "\n" : string.Empty).Append("Marchandises : ").Append(Goods(taken));
        }

        if (lost.Count > 0)
        {
            text.Append(text.Length > 0 ? "\n" : string.Empty).Append("Perdu faute de place : ").Append(Goods(lost));
        }

        return text.ToString();
    }
}

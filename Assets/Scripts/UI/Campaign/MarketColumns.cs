using UnityEngine;

/// <summary>
/// Where the seven columns of the market table stand in each tab: the Buy tab shows the buying price (and not the
/// selling price or the profit), the Sell tab shows the selling price and the profit (and not the buying price). Each
/// entry is (x, width) in the table, or a width of 0 for a column the tab does not show. The header and every line use
/// the same layout, so they always line up.
/// </summary>
public static class MarketColumns
{
    /// <summary>Marchandise, Ville, Vous, Achat, Vente, Profit/u, Tendance.</summary>
    public const int Count = 7;

    private static readonly Vector2[] Buy =
    {
        new Vector2(10f, 350f), new Vector2(370f, 110f), new Vector2(490f, 110f), new Vector2(610f, 170f),
        Vector2.zero, Vector2.zero, new Vector2(790f, 200f)
    };

    private static readonly Vector2[] Sell =
    {
        new Vector2(10f, 330f), new Vector2(350f, 100f), new Vector2(460f, 100f), Vector2.zero,
        new Vector2(570f, 150f), new Vector2(730f, 140f), new Vector2(880f, 110f)
    };

    /// <summary>Places the columns (in the order above) for the tab in use, hiding those it does not show.</summary>
    public static void Apply(RectTransform[] columns, bool buying)
    {
        Vector2[] layout = buying ? Buy : Sell;
        for (int i = 0; i < columns.Length && i < Count; i++)
        {
            bool shown = layout[i].y > 0f;
            columns[i].gameObject.SetActive(shown);
            if (shown)
            {
                columns[i].anchoredPosition = new Vector2(layout[i].x, columns[i].anchoredPosition.y);
                columns[i].sizeDelta = new Vector2(layout[i].y, columns[i].sizeDelta.y);
            }
        }
    }
}

using UnityEngine;
using UnityEngine.UI;
using UImage = UnityEngine.UI.Image;

/// <summary>
/// One line of the market table: a good, what the town has, what the player has, the two prices, what a unit sold here
/// would earn over what it cost (only for a good the player carries) and where the price is heading. It only shows what
/// it is given and says when it is clicked.
/// </summary>
public class MarketRowView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private UImage background;
    [SerializeField] private Text nameText;
    [SerializeField] private Text townStockText;
    [SerializeField] private Text ownedText;
    [SerializeField] private Text buyText;
    [SerializeField] private Text sellText;
    [SerializeField] private Text profitText;
    [SerializeField] private Text trendText;
    [SerializeField] private Color normalColor = new Color(0.22f, 0.25f, 0.32f, 0.95f);
    [SerializeField] private Color selectedColor = new Color(0.30f, 0.45f, 0.62f, 1f);
    [SerializeField] private Color fixedPriceColor = new Color(0.55f, 0.80f, 1f, 1f);
    [SerializeField] private Color risingColor = new Color(1f, 0.65f, 0.35f, 1f);
    [SerializeField] private Color fallingColor = new Color(0.45f, 0.85f, 0.95f, 1f);
    [SerializeField] private Color stableColor = new Color(0.75f, 0.78f, 0.82f, 1f);
    [SerializeField] private Color gainColor = new Color(0.55f, 0.90f, 0.55f, 1f);
    [SerializeField] private Color lossColor = new Color(1f, 0.55f, 0.45f, 1f);

    private RectTransform[] columns;

    public Button Button => button;
    public GoodDefinition Good { get; private set; }

    public void Bind(GoodDefinition good)
    {
        Good = good;
    }

    public void Show(int townStock, int owned, int buyPrice, int sellPrice, TrendReading trend, bool showProfit, int unitProfit)
    {
        bool stable = Good.StablePrice;
        nameText.text = Good.DisplayName + (stable ? "  (cours fixe)" : string.Empty);
        nameText.color = stable ? fixedPriceColor : Color.white;
        townStockText.text = MoneyFormat.Number(townStock);
        ownedText.text = owned > 0 ? MoneyFormat.Number(owned) : "-";
        buyText.text = buyPrice > 0 ? MoneyFormat.Number(buyPrice) : "rupture";
        // 0: the town does not buy this good.
        sellText.text = sellPrice > 0 ? MoneyFormat.Number(sellPrice) : "-";
        if (showProfit)
        {
            profitText.text = TradeSign(unitProfit);
            profitText.color = unitProfit >= 0 ? gainColor : lossColor;
        }
        else
        {
            profitText.text = "-";
            profitText.color = stableColor;
        }

        ShowTrend(trend, stable);
    }

    /// <summary>Lays the columns of this line out for the tab in use (see MarketColumns).</summary>
    public void SetTab(bool buying)
    {
        if (columns == null)
        {
            columns = new[]
            {
                nameText.rectTransform, townStockText.rectTransform, ownedText.rectTransform, buyText.rectTransform,
                sellText.rectTransform, profitText.rectTransform, trendText.rectTransform
            };
        }

        MarketColumns.Apply(columns, buying);
    }

    public void SetSelected(bool selected)
    {
        background.color = selected ? selectedColor : normalColor;
    }

    // "Hausse +12 %", "Baisse -8 %", "Stable", "fixe" for a fixed price, "-" when the action is not possible.
    private void ShowTrend(TrendReading trend, bool stable)
    {
        if (stable)
        {
            trendText.text = "fixe";
            trendText.color = fixedPriceColor;
        }
        else if (!trend.Known)
        {
            trendText.text = "-";
            trendText.color = stableColor;
        }
        else if (trend.Trend == PriceTrend.Rising)
        {
            trendText.text = "Hausse +" + trend.Percent + " %";
            trendText.color = risingColor;
        }
        else if (trend.Trend == PriceTrend.Falling)
        {
            trendText.text = "Baisse " + trend.Percent + " %";
            trendText.color = fallingColor;
        }
        else
        {
            trendText.text = "Stable";
            trendText.color = stableColor;
        }
    }

    private static string TradeSign(int amount)
    {
        return (amount > 0 ? "+" : string.Empty) + MoneyFormat.Number(amount);
    }
}

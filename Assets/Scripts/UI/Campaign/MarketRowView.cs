using UnityEngine;
using UnityEngine.UI;
using UImage = UnityEngine.UI.Image;

/// <summary>
/// One line of the market table: a good, what the town has, what the player has, the two prices and where the price
/// is heading. It only shows what it is given and says when it is clicked.
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
    [SerializeField] private Text trendText;
    [SerializeField] private Color normalColor = new Color(0.22f, 0.25f, 0.32f, 0.95f);
    [SerializeField] private Color selectedColor = new Color(0.30f, 0.45f, 0.62f, 1f);
    [SerializeField] private Color fixedPriceColor = new Color(0.55f, 0.80f, 1f, 1f);
    [SerializeField] private Color risingColor = new Color(1f, 0.65f, 0.35f, 1f);
    [SerializeField] private Color fallingColor = new Color(0.45f, 0.85f, 0.95f, 1f);
    [SerializeField] private Color stableColor = new Color(0.75f, 0.78f, 0.82f, 1f);

    public Button Button => button;
    public GoodDefinition Good { get; private set; }

    public void Bind(GoodDefinition good)
    {
        Good = good;
    }

    public void Show(int townStock, int owned, int buyPrice, int sellPrice, PriceTrend trend)
    {
        bool stable = Good.StablePrice;
        nameText.text = Good.DisplayName + (stable ? "  (cours fixe)" : string.Empty);
        nameText.color = stable ? fixedPriceColor : Color.white;
        townStockText.text = MoneyFormat.Number(townStock);
        ownedText.text = owned > 0 ? MoneyFormat.Number(owned) : "-";
        buyText.text = buyPrice > 0 ? MoneyFormat.Number(buyPrice) : "rupture";
        sellText.text = MoneyFormat.Number(sellPrice);
        switch (trend)
        {
            case PriceTrend.Rising:
                trendText.text = stable ? "fixe" : "Hausse";
                trendText.color = risingColor;
                break;
            case PriceTrend.Falling:
                trendText.text = "Baisse";
                trendText.color = fallingColor;
                break;
            default:
                trendText.text = stable ? "fixe" : "Stable";
                trendText.color = stable ? fixedPriceColor : stableColor;
                break;
        }
    }

    public void SetSelected(bool selected)
    {
        background.color = selected ? selectedColor : normalColor;
    }
}

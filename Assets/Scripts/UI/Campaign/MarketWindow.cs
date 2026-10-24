using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The market window of a town, with two tabs: Buy and Sell. Both show every good in a table (what the town has, what
/// the player has, the two prices, what a unit sold here would earn over what it cost, where the price is heading), a
/// selection with its quantity (1, +1, +10, Max) and, for the tab in use, a preview of what the trade would come to, the
/// button to do it and the reason written when it cannot be done. Max is the most that can be bought in the Buy tab and
/// everything owned in the Sell tab. It works only through the trade service and the events (a trade, the money, the
/// cargo, the market): it holds no rule and works nothing out. Each action asks the service, which also has the last
/// word.
/// </summary>
public class MarketWindow : TownFunctionWindow
{
    // Only so the quantity stays readable on screen; the service says what is really possible.
    private const int MaxShownQuantity = 9999;

    [SerializeField] private TradeService tradeService;
    [SerializeField] private Wallet wallet;
    [SerializeField] private Inventory inventory;
    [SerializeField] private Text titleText;
    [Header("Tabs")]
    [SerializeField] private Button buyTabButton;
    [SerializeField] private Button sellTabButton;
    [SerializeField] private Color tabSelectedColor = new Color(0.30f, 0.45f, 0.62f, 1f);
    [SerializeField] private Color tabNormalColor = new Color(0.22f, 0.25f, 0.32f, 0.95f);
    [Header("Table")]
    [Tooltip("The seven header texts, in the order of the columns (see MarketColumns).")]
    [SerializeField] private RectTransform[] headerColumns;
    [Tooltip("An inactive line used as a model.")]
    [SerializeField] private MarketRowView rowTemplate;
    [SerializeField] private RectTransform rowContainer;
    [Header("Selection")]
    [SerializeField] private Text selectedText;
    [SerializeField] private Text quantityText;
    [SerializeField] private Button oneButton;
    [SerializeField] private Button plusOneButton;
    [SerializeField] private Button plusTenButton;
    [UnityEngine.Serialization.FormerlySerializedAs("maxBuyButton")]
    [SerializeField] private Button maxButton;
    [Header("Buying and selling (one of the two is shown, the same place)")]
    [SerializeField] private Text buyPreviewText;
    [SerializeField] private Text buyReasonText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Text sellPreviewText;
    [SerializeField] private Text sellReasonText;
    [SerializeField] private Button sellButton;
    [SerializeField] private Text resultText;
    [SerializeField] private Color possibleColor = new Color(0.55f, 0.90f, 0.55f, 1f);
    [SerializeField] private Color refusedColor = new Color(1f, 0.55f, 0.45f, 1f);
    [SerializeField] private Color gainColor = new Color(0.55f, 0.90f, 0.55f, 1f);
    [SerializeField] private Color lossColor = new Color(1f, 0.55f, 0.45f, 1f);

    private readonly List<MarketRowView> rows = new List<MarketRowView>();
    private readonly StringBuilder text = new StringBuilder(200);
    private Market market;
    private GoodDefinition selected;
    private TradeKind tab = TradeKind.Buy;
    private int buyQuantity = 1;
    private int sellQuantity = 1;

    private int Quantity => tab == TradeKind.Buy ? buyQuantity : sellQuantity;

    protected override void Start()
    {
        base.Start();
        if (!enabled)
        {
            return;
        }

        if (tradeService == null || wallet == null || inventory == null || titleText == null || buyTabButton == null
            || sellTabButton == null || headerColumns == null || headerColumns.Length != MarketColumns.Count || rowTemplate == null || rowContainer == null || selectedText == null
            || quantityText == null || oneButton == null || plusOneButton == null || plusTenButton == null || maxButton == null
            || buyPreviewText == null || buyReasonText == null || buyButton == null || sellPreviewText == null
            || sellReasonText == null || sellButton == null || resultText == null)
        {
            Debug.LogError("MarketWindow: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        rowTemplate.gameObject.SetActive(false);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (enabled && oneButton != null)
        {
            buyTabButton.onClick.AddListener(OnBuyTab);
            sellTabButton.onClick.AddListener(OnSellTab);
            oneButton.onClick.AddListener(OnOne);
            plusOneButton.onClick.AddListener(OnPlusOne);
            plusTenButton.onClick.AddListener(OnPlusTen);
            maxButton.onClick.AddListener(OnMax);
            buyButton.onClick.AddListener(OnBuy);
            sellButton.onClick.AddListener(OnSell);
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (oneButton != null)
        {
            buyTabButton.onClick.RemoveListener(OnBuyTab);
            sellTabButton.onClick.RemoveListener(OnSellTab);
            oneButton.onClick.RemoveListener(OnOne);
            plusOneButton.onClick.RemoveListener(OnPlusOne);
            plusTenButton.onClick.RemoveListener(OnPlusTen);
            maxButton.onClick.RemoveListener(OnMax);
            buyButton.onClick.RemoveListener(OnBuy);
            sellButton.onClick.RemoveListener(OnSell);
        }
    }

    protected override void OnOpened(PointOfInterest town)
    {
        market = Market.For(town);
        if (market == null)
        {
            Debug.LogError("MarketWindow: the town '" + town.DisplayName + "' has no market.", this);
            titleText.text = "Pas de marché ici";
            return;
        }

        titleText.text = "Marché de " + town.DisplayName;
        BuildRows();
        selected = market.AllGoods[0];
        tab = TradeKind.Buy;
        buyQuantity = 1;
        sellQuantity = 1;
        resultText.text = string.Empty;

        tradeService.Traded += OnTraded;
        wallet.Changed += Refresh;
        inventory.Changed += Refresh;
        market.Changed += Refresh;
        Refresh();
    }

    protected override void OnClosing()
    {
        tradeService.Traded -= OnTraded;
        wallet.Changed -= Refresh;
        inventory.Changed -= Refresh;
        if (market != null)
        {
            market.Changed -= Refresh;
        }

        market = null;
    }

    private void BuildRows()
    {
        IReadOnlyList<GoodDefinition> goods = market.AllGoods;
        if (rows.Count == goods.Count)
        {
            return;
        }

        for (int i = 0; i < goods.Count; i++)
        {
            MarketRowView row = Instantiate(rowTemplate, rowContainer);
            row.gameObject.SetActive(true);
            row.Bind(goods[i]);
            GoodDefinition good = goods[i];
            row.Button.onClick.AddListener(() => Select(good));
            rows.Add(row);
        }
    }

    private void Select(GoodDefinition good)
    {
        selected = good;
        Refresh();
    }

    private void OnBuyTab()
    {
        tab = TradeKind.Buy;
        Refresh();
    }

    private void OnSellTab()
    {
        tab = TradeKind.Sell;
        Refresh();
    }

    private void OnOne()
    {
        SetQuantity(1);
    }

    private void OnPlusOne()
    {
        SetQuantity(Quantity + 1);
    }

    private void OnPlusTen()
    {
        SetQuantity(Quantity + 10);
    }

    // The most that can be bought (Buy tab) or the most that is owned (Sell tab), as the service says it.
    private void OnMax()
    {
        SetQuantity(tradeService.MaxQuantity(tab, market, selected, out _));
    }

    private void SetQuantity(int value)
    {
        // At least 1: the preview then says why a 'maximum' of 0 is not possible.
        int clamped = Mathf.Clamp(value, 1, MaxShownQuantity);
        if (tab == TradeKind.Buy)
        {
            buyQuantity = clamped;
        }
        else
        {
            sellQuantity = clamped;
        }

        Refresh();
    }

    private void OnBuy()
    {
        TradeResult result = tradeService.Buy(market, selected, buyQuantity);
        if (!result.Success)
        {
            resultText.text = TradeMessages.Describe(result);
        }
    }

    private void OnSell()
    {
        TradeResult result = tradeService.Sell(market, selected, sellQuantity);
        if (!result.Success)
        {
            resultText.text = TradeMessages.Describe(result);
        }
    }

    private void OnTraded(TradeRecord record)
    {
        resultText.text = TradeMessages.Describe(record);
    }

    private void Refresh()
    {
        if (market == null)
        {
            return;
        }

        bool buying = tab == TradeKind.Buy;
        EnsureSelectionListed(buying);
        MarketColumns.Apply(headerColumns, buying);
        for (int i = 0; i < rows.Count; i++)
        {
            MarketRowView row = rows[i];
            GoodDefinition good = row.Good;
            int owned = inventory.GetQuantity(good);
            row.Show(market.GetStock(good), owned,
                tradeService.UnitPrice(TradeKind.Buy, market, good), tradeService.UnitPrice(TradeKind.Sell, market, good),
                tradeService.GetTrend(market, good, tab, Quantity), owned > 0, tradeService.UnitProfit(market, good));
            // Buy tab: only what the town offers (in stock or made there), with the buying price. Sell tab: everything the
            // town buys, which is every good, with the selling price and the profit.
            row.gameObject.SetActive(!buying || market.IsOffered(good));
            row.SetTab(buying);
            row.SetSelected(good == selected);
        }

        buyTabButton.image.color = buying ? tabSelectedColor : tabNormalColor;
        sellTabButton.image.color = buying ? tabNormalColor : tabSelectedColor;
        buyPreviewText.gameObject.SetActive(buying);
        buyReasonText.gameObject.SetActive(buying);
        buyButton.gameObject.SetActive(buying);
        sellPreviewText.gameObject.SetActive(!buying);
        sellReasonText.gameObject.SetActive(!buying);
        sellButton.gameObject.SetActive(!buying);

        selectedText.text = selected.DisplayName + "  -  1 " + selected.Unit + " = "
            + selected.MassPerUnit.ToString("0.##", CultureInfo.InvariantCulture) + " kg, "
            + selected.VolumePerUnit.ToString("0.##", CultureInfo.InvariantCulture) + " L"
            + (selected.StablePrice ? "  -  cours fixe" : string.Empty);
        quantityText.text = "Quantité : " + MoneyFormat.Number(Quantity);
        if (buying)
        {
            ShowPreview(tradeService.Preview(TradeKind.Buy, market, selected, buyQuantity), buyPreviewText, buyReasonText, buyButton);
        }
        else
        {
            ShowPreview(tradeService.Preview(TradeKind.Sell, market, selected, sellQuantity), sellPreviewText, sellReasonText, sellButton);
        }
    }

    // The selected good must be one of the lines listed in the tab in use; if not, the first listed one is chosen.
    private void EnsureSelectionListed(bool buying)
    {
        if (!buying || market.IsOffered(selected))
        {
            return;
        }

        IReadOnlyList<GoodDefinition> goods = market.AllGoods;
        for (int i = 0; i < goods.Count; i++)
        {
            if (market.IsOffered(goods[i]))
            {
                selected = goods[i];
                return;
            }
        }
    }

    private void ShowPreview(TradePreview preview, Text previewText, Text reasonText, Button button)
    {
        bool selling = preview.Kind == TradeKind.Sell;
        text.Clear().Append(selling ? "Vente" : "Achat").Append(" de ").Append(MoneyFormat.Number(preview.Quantity)).Append(" : ")
            .Append(preview.Amount > 0 ? MoneyFormat.WithUnit(preview.Amount) : "-");
        if (selling && preview.Success)
        {
            // The profit line alone is colored (rich text): green for a gain, red for a loss.
            Color profitColor = preview.Profit >= 0 ? gainColor : lossColor;
            text.Append("\n<color=#").Append(ColorUtility.ToHtmlStringRGB(profitColor)).Append(">Profit : ")
                .Append(TradeMessages.SignedMoney(preview.Profit))
                .Append(preview.Cost > 0 ? " (" + (preview.ProfitPercent > 0 ? "+" : string.Empty) + preview.ProfitPercent + " %), coût " + MoneyFormat.WithUnit(preview.Cost)
                    : "  -  sans coût d'achat")
                .Append("</color>");
        }

        text.Append("\nSolde après : ").Append(MoneyFormat.WithUnit(preview.BalanceAfter))
            .Append("\nPlace restante : ").Append(preview.MassLeftAfter.ToString("0.#", CultureInfo.InvariantCulture)).Append(" kg, ")
            .Append(preview.VolumeLeftAfter.ToString("0.#", CultureInfo.InvariantCulture)).Append(" L");
        previewText.text = text.ToString();
        reasonText.text = TradeMessages.Describe(preview);
        reasonText.color = preview.Success ? possibleColor : refusedColor;
        button.interactable = preview.Success;
    }
}

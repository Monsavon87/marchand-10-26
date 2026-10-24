using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The market window of a town: one line per good (what the town has, what the player has, the two prices, where the
/// price is heading), a selection with its quantity, a preview of what buying and selling would come to, and the
/// buttons to do it, with the reason written when it cannot be done. It works only through the trade service and the
/// events (a trade, the money, the cargo, the market): it holds no rule and works nothing out. Each action asks the
/// service, which also has the last word.
/// </summary>
public class MarketWindow : TownFunctionWindow
{
    // Only so the quantity stays readable on screen; the service says what is really possible.
    private const int MaxShownQuantity = 9999;

    [SerializeField] private TradeService tradeService;
    [SerializeField] private Wallet wallet;
    [SerializeField] private Inventory inventory;
    [SerializeField] private Text titleText;
    [Tooltip("An inactive line used as a model.")]
    [SerializeField] private MarketRowView rowTemplate;
    [SerializeField] private RectTransform rowContainer;
    [Header("Selection")]
    [SerializeField] private Text selectedText;
    [SerializeField] private Text quantityText;
    [SerializeField] private Button oneButton;
    [SerializeField] private Button plusOneButton;
    [SerializeField] private Button plusTenButton;
    [SerializeField] private Button maxBuyButton;
    [SerializeField] private Button sellAllButton;
    [Header("Buying and selling")]
    [SerializeField] private Text buyPreviewText;
    [SerializeField] private Text buyReasonText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Text sellPreviewText;
    [SerializeField] private Text sellReasonText;
    [SerializeField] private Button sellButton;
    [SerializeField] private Text resultText;
    [SerializeField] private Color possibleColor = new Color(0.55f, 0.90f, 0.55f, 1f);
    [SerializeField] private Color refusedColor = new Color(1f, 0.55f, 0.45f, 1f);

    private readonly List<MarketRowView> rows = new List<MarketRowView>();
    private readonly StringBuilder text = new StringBuilder(200);
    private Market market;
    private GoodDefinition selected;
    private int quantity = 1;

    protected override void Start()
    {
        base.Start();
        if (!enabled)
        {
            return;
        }

        if (tradeService == null || wallet == null || inventory == null || titleText == null || rowTemplate == null
            || rowContainer == null || selectedText == null || quantityText == null || oneButton == null || plusOneButton == null
            || plusTenButton == null || maxBuyButton == null || sellAllButton == null || buyPreviewText == null
            || buyReasonText == null || buyButton == null || sellPreviewText == null || sellReasonText == null
            || sellButton == null || resultText == null)
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
            oneButton.onClick.AddListener(OnOne);
            plusOneButton.onClick.AddListener(OnPlusOne);
            plusTenButton.onClick.AddListener(OnPlusTen);
            maxBuyButton.onClick.AddListener(OnMaxBuy);
            sellAllButton.onClick.AddListener(OnSellAll);
            buyButton.onClick.AddListener(OnBuy);
            sellButton.onClick.AddListener(OnSell);
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (oneButton != null)
        {
            oneButton.onClick.RemoveListener(OnOne);
            plusOneButton.onClick.RemoveListener(OnPlusOne);
            plusTenButton.onClick.RemoveListener(OnPlusTen);
            maxBuyButton.onClick.RemoveListener(OnMaxBuy);
            sellAllButton.onClick.RemoveListener(OnSellAll);
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
        quantity = 1;
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

    private void OnOne()
    {
        SetQuantity(1);
    }

    private void OnPlusOne()
    {
        SetQuantity(quantity + 1);
    }

    private void OnPlusTen()
    {
        SetQuantity(quantity + 10);
    }

    private void OnMaxBuy()
    {
        SetQuantity(tradeService.MaxQuantity(TradeKind.Buy, market, selected, out _));
    }

    private void OnSellAll()
    {
        SetQuantity(tradeService.MaxQuantity(TradeKind.Sell, market, selected, out _));
    }

    private void SetQuantity(int value)
    {
        // At least 1: the preview then says why a 'maximum' of 0 is not possible.
        quantity = Mathf.Clamp(value, 1, MaxShownQuantity);
        Refresh();
    }

    private void OnBuy()
    {
        TradeResult result = tradeService.Buy(market, selected, quantity);
        if (!result.Success)
        {
            resultText.text = TradeMessages.Describe(result);
        }
    }

    private void OnSell()
    {
        TradeResult result = tradeService.Sell(market, selected, quantity);
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

        for (int i = 0; i < rows.Count; i++)
        {
            MarketRowView row = rows[i];
            GoodDefinition good = row.Good;
            row.Show(market.GetStock(good), inventory.GetQuantity(good),
                tradeService.UnitPrice(TradeKind.Buy, market, good), tradeService.UnitPrice(TradeKind.Sell, market, good),
                tradeService.GetTrend(market, good));
            row.SetSelected(good == selected);
        }

        selectedText.text = selected.DisplayName + "  -  1 " + selected.Unit + " = "
            + selected.MassPerUnit.ToString("0.##", CultureInfo.InvariantCulture) + " kg, "
            + selected.VolumePerUnit.ToString("0.##", CultureInfo.InvariantCulture) + " L"
            + (selected.StablePrice ? "  -  cours fixe" : string.Empty);
        quantityText.text = "Quantité : " + MoneyFormat.Number(quantity);
        ShowPreview(tradeService.Preview(TradeKind.Buy, market, selected, quantity), buyPreviewText, buyReasonText, buyButton);
        ShowPreview(tradeService.Preview(TradeKind.Sell, market, selected, quantity), sellPreviewText, sellReasonText, sellButton);
    }

    private void ShowPreview(TradePreview preview, Text previewText, Text reasonText, Button button)
    {
        string verb = preview.Kind == TradeKind.Buy ? "Achat" : "Vente";
        text.Clear().Append(verb).Append(" de ").Append(MoneyFormat.Number(preview.Quantity)).Append(" : ")
            .Append(preview.Amount > 0 ? MoneyFormat.WithUnit(preview.Amount) : "-")
            .Append("\nSolde après : ").Append(MoneyFormat.WithUnit(preview.BalanceAfter))
            .Append("\nPlace restante : ").Append(preview.MassLeftAfter.ToString("0.#", CultureInfo.InvariantCulture)).Append(" kg, ")
            .Append(preview.VolumeLeftAfter.ToString("0.#", CultureInfo.InvariantCulture)).Append(" L");
        previewText.text = text.ToString();
        reasonText.text = TradeMessages.Describe(preview);
        reasonText.color = preview.Success ? possibleColor : refusedColor;
        button.interactable = preview.Success;
    }
}

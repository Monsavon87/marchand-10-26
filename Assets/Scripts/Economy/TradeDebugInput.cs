using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Debug only: keys to trade without an interface, in the town shown by the price table (its town keys change it).
/// Hold the buy key (B) or the sell key (V) and press a digit, 1 to 0 for the catalog's 1st to 10th good; hold Shift
/// as well for the most possible. Each try is written in the console with its status, the quantity really traded,
/// the amount and the totals. Remove or disable it once the trade interface exists.
/// </summary>
public class TradeDebugInput : MonoBehaviour
{
    private static readonly Key[] GoodKeys =
    {
        Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
        Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9, Key.Digit0
    };

    [SerializeField] private TradeService tradeService;
    [SerializeField] private PriceDebugView priceView;
    [SerializeField] private Wallet wallet;
    [SerializeField] private Inventory inventory;
    [SerializeField] private GoodCatalog catalog;
    [Tooltip("Keys are physical positions (US layout).")]
    [SerializeField] private Key buyKey = Key.B;
    [SerializeField] private Key sellKey = Key.V;
    [Tooltip("Units bought or sold by each key press (the most possible with Shift).")]
    [Min(1)] [SerializeField] private int quantity = 1;

    private void Start()
    {
        if (tradeService == null || priceView == null || wallet == null || inventory == null || catalog == null)
        {
            Debug.LogError("TradeDebugInput: a serialized reference is not assigned.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        bool buy = keyboard[buyKey].isPressed;
        bool sell = keyboard[sellKey].isPressed;
        if (buy == sell)
        {
            return;
        }

        bool max = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        for (int i = 0; i < GoodKeys.Length && i < catalog.Goods.Count; i++)
        {
            if (keyboard[GoodKeys[i]].wasPressedThisFrame)
            {
                Try(catalog.Goods[i], buy, max);
            }
        }
    }

    private void Try(GoodDefinition good, bool buy, bool max)
    {
        Market market = priceView.SelectedMarket;
        if (market == null)
        {
            Debug.LogWarning("[Debug commerce] no town selected: open the price table (its key) first.", this);
            return;
        }

        TradeResult result;
        if (buy)
        {
            result = max ? tradeService.BuyMax(market, good) : tradeService.Buy(market, good, quantity);
        }
        else
        {
            result = max ? tradeService.SellMax(market, good) : tradeService.Sell(market, good, quantity);
        }

        Debug.Log("[Debug commerce] " + (buy ? "achat" : "vente") + (max ? " max " : " ") + good.DisplayName + " a " + market.TownName
            + " -> " + result.Status + " (" + TradeMessages.Describe(result) + ")"
            + " | quantité " + result.Quantity + " | montant " + MoneyFormat.WithUnit(result.Amount)
            + " | solde " + MoneyFormat.WithUnit(wallet.Balance)
            + " | possédé " + inventory.GetQuantity(good)
            + " | masse " + inventory.TotalMass.ToString("0.###") + "/" + inventory.Capacity.MaxMass
            + " | volume " + inventory.TotalVolume.ToString("0.###") + "/" + inventory.Capacity.MaxVolume
            + " | stock ville " + market.GetStock(good), this);
    }
}

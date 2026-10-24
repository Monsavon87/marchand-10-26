using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Debug only: a key shows or hides a table of the prices of ONE town (two keys change the town): for each good it
/// trades, the stock, the price factor, the price before the margin, what one unit costs or brings, and the total
/// for a batch of units (the price moves during the batch). A * marks a good with a fixed price. Remove or disable
/// it once the trade interface exists.
/// </summary>
public class PriceDebugView : MonoBehaviour
{
    private const float NameWidth = 130f;
    private const float CellWidth = 100f;
    private const int ColumnCount = 8;

    [SerializeField] private PricingSettings settings;
    [Tooltip("Keys are physical positions (US layout).")]
    [SerializeField] private Key toggleKey = Key.F4;
    [SerializeField] private Key previousTownKey = Key.PageUp;
    [SerializeField] private Key nextTownKey = Key.PageDown;
    [Tooltip("The size of the batch whose total is shown.")]
    [Min(2)] [SerializeField] private int batchSize = 10;

    private readonly List<Market> markets = new List<Market>();
    private readonly StringBuilder cell = new StringBuilder(32);
    private IPricingRules rules;
    private string[] rowNames;
    private string[,] cells;
    private string[] headers;
    private int townIndex;
    private bool visible;
    private bool dirty = true;
    private GUIStyle style;

    private void Start()
    {
        if (settings == null)
        {
            Debug.LogError("PriceDebugView: the pricing settings are not assigned.", this);
            enabled = false;
            return;
        }

        markets.AddRange(Market.All);
        if (markets.Count == 0)
        {
            Debug.LogError("PriceDebugView: there is no market in the scene.", this);
            enabled = false;
            return;
        }

        markets.Sort((a, b) => string.CompareOrdinal(a.TownName, b.TownName));
        rules = new PricingRules(settings);
        settings.Validate();
        for (int i = 0; i < markets.Count; i++)
        {
            markets[i].Changed += MarkDirty;
        }

        headers = new[]
        {
            "Stock / cible", "Facteur", "Prix moyen", "Achat x1", "Vente x1",
            "Achat x" + batchSize, "Vente x" + batchSize, "Écart achat-vente"
        };
    }

    private void OnDestroy()
    {
        for (int i = 0; i < markets.Count; i++)
        {
            if (markets[i] != null)
            {
                markets[i].Changed -= MarkDirty;
            }
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard[toggleKey].wasPressedThisFrame)
        {
            visible = !visible;
        }

        if (keyboard[nextTownKey].wasPressedThisFrame)
        {
            townIndex = (townIndex + 1) % markets.Count;
            dirty = true;
        }

        if (keyboard[previousTownKey].wasPressedThisFrame)
        {
            townIndex = (townIndex + markets.Count - 1) % markets.Count;
            dirty = true;
        }
    }

    private void OnGUI()
    {
        if (!visible)
        {
            return;
        }

        if (dirty)
        {
            Rebuild();
        }

        if (style == null)
        {
            style = new GUIStyle(GUI.skin.label) { fontSize = 13 };
        }

        float width = NameWidth + CellWidth * ColumnCount + 30f;
        float height = 100f + 25f * rowNames.Length;
        GUILayout.BeginArea(new Rect(Screen.width - width - 10f, 80f, width, height), GUI.skin.box);
        Market market = markets[townIndex];
        GUILayout.Label("Prix de " + market.TownName + " (" + market.Definition.ProfileName + ") - " + toggleKey
            + " affiche/masque, " + previousTownKey + "/" + nextTownKey + " change de ville, * = prix fixe", style);

        GUILayout.BeginHorizontal();
        GUILayout.Label(string.Empty, style, GUILayout.Width(NameWidth));
        for (int c = 0; c < ColumnCount; c++)
        {
            GUILayout.Label(headers[c], style, GUILayout.Width(CellWidth));
        }

        GUILayout.EndHorizontal();
        for (int g = 0; g < rowNames.Length; g++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(rowNames[g], style, GUILayout.Width(NameWidth));
            for (int c = 0; c < ColumnCount; c++)
            {
                GUILayout.Label(cells[g, c], style, GUILayout.Width(CellWidth));
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.EndArea();
    }

    private void MarkDirty()
    {
        dirty = true;
    }

    private void Rebuild()
    {
        Market market = markets[townIndex];
        IReadOnlyList<GoodStockProfile> entries = market.Entries;
        rowNames = new string[entries.Count];
        cells = new string[entries.Count, ColumnCount];
        for (int g = 0; g < entries.Count; g++)
        {
            GoodDefinition good = entries[g].good;
            int target = entries[g].targetStock;
            int stock = market.GetStock(good);
            rowNames[g] = good.DisplayName + (good.StablePrice ? " *" : string.Empty);

            int buyOne = rules.BuyUnitPrice(good, stock, target);
            int sellOne = rules.SellUnitPrice(good, stock, target);
            cells[g, 0] = stock + " / " + target;
            cells[g, 1] = good.StablePrice ? "fixe" : rules.PriceFactor(good, stock, target).ToString("0.00");
            cells[g, 2] = MoneyFormat.Number(rules.MidPrice(good, stock, target));
            cells[g, 3] = stock > 0 ? MoneyFormat.Number(buyOne) : "rupture";
            cells[g, 4] = MoneyFormat.Number(sellOne);
            cells[g, 5] = rules.TryQuoteBuy(good, stock, target, batchSize, out int buyTotal) == PriceQuoteResult.Accepted
                ? MoneyFormat.Number(buyTotal) : "stock < " + batchSize;
            cells[g, 6] = rules.TryQuoteSell(good, stock, target, batchSize, out int sellTotal) == PriceQuoteResult.Accepted
                ? MoneyFormat.Number(sellTotal) : "-";
            cell.Clear();
            if (stock > 0)
            {
                cell.Append(Mathf.RoundToInt(100f * (buyOne - sellOne) / buyOne)).Append(" %");
            }
            else
            {
                cell.Append('-');
            }

            cells[g, 7] = cell.ToString();
        }

        dirty = false;
    }
}

using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Debug only: a key shows or hides a table of every market's stock (goods in rows, towns in columns, each cell
/// "stock/target +made -used per day"; a * marks a good with a fixed price). It also runs the check of the whole
/// market network once at the start, which writes warnings in the console. Remove or disable it once the trade
/// interface exists.
/// </summary>
public class MarketDebugView : MonoBehaviour
{
    private const float CellWidth = 125f;
    private const float NameWidth = 90f;

    [SerializeField] private GoodCatalog catalog;
    [Tooltip("Keys are physical positions (US layout).")]
    [SerializeField] private Key toggleKey = Key.F3;

    private readonly List<Market> markets = new List<Market>();
    private readonly StringBuilder cell = new StringBuilder(32);
    private string[] columnHeaders;
    private string[] rowNames;
    private string[,] cells;
    private bool visible;
    private bool dirty = true;
    private GUIStyle style;

    private void Start()
    {
        if (catalog == null)
        {
            Debug.LogError("MarketDebugView: the catalog is not assigned.", this);
            enabled = false;
            return;
        }

        markets.AddRange(Market.All);
        if (markets.Count == 0)
        {
            Debug.LogError("MarketDebugView: there is no market in the scene.", this);
            enabled = false;
            return;
        }

        for (int i = 0; i < markets.Count; i++)
        {
            markets[i].Changed += MarkDirty;
        }

        int problems = MarketNetworkCheck.Validate(markets, catalog);
        if (problems == 0)
        {
            Debug.Log("[Markets] " + markets.Count + " markets, network check: no problem.", this);
        }
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
        if (keyboard != null && keyboard[toggleKey].wasPressedThisFrame)
        {
            visible = !visible;
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

        // Title, two-line header, one line per good, and the box's padding.
        float width = NameWidth + CellWidth * markets.Count + 30f;
        float height = 100f + 25f * rowNames.Length;
        GUILayout.BeginArea(new Rect(10f, 80f, width, height), GUI.skin.box);
        GUILayout.Label("Marchés (" + toggleKey + ") : stock/cible +production -consommation par jour, * = prix fixe", style);

        GUILayout.BeginHorizontal();
        GUILayout.Label(string.Empty, style, GUILayout.Width(NameWidth));
        for (int m = 0; m < markets.Count; m++)
        {
            GUILayout.Label(columnHeaders[m], style, GUILayout.Width(CellWidth));
        }

        GUILayout.EndHorizontal();
        for (int g = 0; g < rowNames.Length; g++)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(rowNames[g], style, GUILayout.Width(NameWidth));
            for (int m = 0; m < markets.Count; m++)
            {
                GUILayout.Label(cells[g, m], style, GUILayout.Width(CellWidth));
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
        IReadOnlyList<GoodDefinition> goods = catalog.Goods;
        columnHeaders = new string[markets.Count];
        rowNames = new string[goods.Count];
        cells = new string[goods.Count, markets.Count];
        for (int m = 0; m < markets.Count; m++)
        {
            columnHeaders[m] = markets[m].TownName + " (" + markets[m].Definition.ProfileName + ")";
        }

        for (int g = 0; g < goods.Count; g++)
        {
            GoodDefinition good = goods[g];
            rowNames[g] = good.DisplayName + (good.StablePrice ? " *" : string.Empty);
            for (int m = 0; m < markets.Count; m++)
            {
                if (!markets[m].Definition.TryGetEntry(good, out GoodStockProfile entry))
                {
                    // Not in the profile: the town has stock only if somebody sold it some.
                    int extra = markets[m].GetStock(good);
                    cells[g, m] = extra > 0 ? extra + " (hors profil)" : "-";
                    continue;
                }

                cell.Clear().Append(markets[m].GetStock(good)).Append('/').Append(entry.targetStock)
                    .Append(" +").Append(entry.productionPerDay).Append(" -").Append(entry.consumptionPerDay);
                cells[g, m] = cell.ToString();
            }
        }

        dirty = false;
    }
}

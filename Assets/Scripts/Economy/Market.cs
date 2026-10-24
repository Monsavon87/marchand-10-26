using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The market of a town: the stock of each good it trades, taken from its TownDefinition when the game starts.
/// Sits on the same object as the town's PointOfInterest. The stock is never negative. TryAddStock and
/// TryRemoveStock are the only ways to change it, each all-or-nothing; only the trade service (a later step) is
/// meant to call them. Pure data plus its rules: it knows nothing about prices or the interface.
/// </summary>
public class Market : MonoBehaviour
{
    private static readonly List<Market> AllMarkets = new List<Market>();

    [SerializeField] private PointOfInterest town;
    [SerializeField] private TownDefinition definition;
    [SerializeField] private GoodCatalog catalog;

    private readonly Dictionary<GoodDefinition, int> stocks = new Dictionary<GoodDefinition, int>();
    private bool configured;

    /// <summary>Every enabled, configured market.</summary>
    public static IReadOnlyList<Market> All => AllMarkets;

    /// <summary>Raised after the stock changed (an add or a remove that was accepted).</summary>
    public event Action Changed = delegate { };

    public PointOfInterest Town => town;
    public TownDefinition Definition => definition;
    public string TownName => town != null ? town.DisplayName : name;
    public IReadOnlyList<GoodStockProfile> Entries => definition.Entries;

    private void Awake()
    {
        if (town == null || definition == null || catalog == null)
        {
            Debug.LogError("Market: the town, the profile or the catalog is not assigned.", this);
            return;
        }

        Validate();
        IReadOnlyList<GoodStockProfile> entries = definition.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            GoodStockProfile entry = entries[i];
            // Entries that Validate has warned about (no good, twice the same good) are left out.
            if (entry != null && entry.good != null && !stocks.ContainsKey(entry.good))
            {
                stocks.Add(entry.good, Mathf.Max(0, entry.initialStock));
            }
        }

        configured = true;
    }

    private void OnEnable()
    {
        if (configured)
        {
            AllMarkets.Add(this);
        }
    }

    private void OnDisable()
    {
        AllMarkets.Remove(this);
    }

    /// <summary>True when the town trades this good.</summary>
    public bool IsTraded(GoodDefinition good)
    {
        return good != null && stocks.ContainsKey(good);
    }

    /// <summary>Units of this good in stock (0 when none, or when the town does not trade it).</summary>
    public int GetStock(GoodDefinition good)
    {
        return good != null && stocks.TryGetValue(good, out int stock) ? stock : 0;
    }

    /// <summary>Adds units to the stock. Accepted, or the reason why nothing was added.</summary>
    public MarketResult TryAddStock(GoodDefinition good, int quantity)
    {
        MarketResult invalid = CheckRequest(good, quantity);
        if (invalid != MarketResult.Accepted)
        {
            return invalid;
        }

        int stock = stocks[good];
        if (quantity > int.MaxValue - stock)
        {
            return MarketResult.Overflow;
        }

        stocks[good] = stock + quantity;
        Changed();
        return MarketResult.Accepted;
    }

    /// <summary>Takes units from the stock. Accepted, or the reason why nothing was taken.</summary>
    public MarketResult TryRemoveStock(GoodDefinition good, int quantity)
    {
        MarketResult invalid = CheckRequest(good, quantity);
        if (invalid != MarketResult.Accepted)
        {
            return invalid;
        }

        int stock = stocks[good];
        if (quantity > stock)
        {
            return MarketResult.InsufficientStock;
        }

        stocks[good] = stock - quantity;
        Changed();
        return MarketResult.Accepted;
    }

    /// <summary>Checks the profile against the catalog and writes a warning for each problem. Returns how many.</summary>
    public int Validate()
    {
        int problems = definition.Validate();
        IReadOnlyList<GoodStockProfile> entries = definition.Entries;
        for (int i = 0; i < entries.Count; i++)
        {
            GoodStockProfile entry = entries[i];
            if (entry == null || entry.good == null)
            {
                continue;
            }

            if (!catalog.TryGetGood(entry.good.Id, out GoodDefinition known) || known != entry.good)
            {
                Debug.LogWarning("Market '" + TownName + "': the good '" + entry.good.name + "' is not in the catalog.", this);
                problems++;
            }
        }

        return problems;
    }

    private MarketResult CheckRequest(GoodDefinition good, int quantity)
    {
        if (!configured)
        {
            return MarketResult.NotConfigured;
        }

        if (!IsTraded(good))
        {
            return MarketResult.NotTraded;
        }

        return quantity > 0 ? MarketResult.Accepted : MarketResult.InvalidQuantity;
    }
}

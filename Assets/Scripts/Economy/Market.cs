using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The market of a town: the stock of each good, started from the town's TownDefinition. Sits on the same object
/// as the town's PointOfInterest. The town buys every good of the catalog: a good outside its profile simply has no
/// stock until somebody sells it some. The stock is never negative. TryAddStock and TryRemoveStock are the only
/// ways to change it, each all-or-nothing, and CheckAddStock / CheckRemoveStock answer the same question without
/// changing anything; only the trade service is meant to call the Try methods. Pure data plus its rules: it knows
/// nothing about prices or the interface.
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
    /// <summary>The goods the town trades (its profile).</summary>
    public IReadOnlyList<GoodStockProfile> Entries => definition.Entries;
    /// <summary>Every good the town buys and sells: the whole catalog.</summary>
    public IReadOnlyList<GoodDefinition> AllGoods => catalog.Goods;

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

    /// <summary>True when the good is in the town's profile (the town makes or uses it).</summary>
    public bool IsInProfile(GoodDefinition good)
    {
        return good != null && definition.TryGetEntry(good, out _);
    }

    /// <summary>Units of this good in stock (0 when none).</summary>
    public int GetStock(GoodDefinition good)
    {
        return good != null && stocks.TryGetValue(good, out int stock) ? stock : 0;
    }

    /// <summary>The stock the price of this good is measured against: the profile's target, or the default one.</summary>
    public int GetTarget(GoodDefinition good)
    {
        return good != null && definition.TryGetEntry(good, out GoodStockProfile entry)
            ? entry.targetStock
            : definition.DefaultTargetStock;
    }

    /// <summary>Would adding this quantity to the stock be accepted? Changes nothing.</summary>
    public MarketResult CheckAddStock(GoodDefinition good, int quantity)
    {
        MarketResult invalid = CheckRequest(good, quantity);
        if (invalid != MarketResult.Accepted)
        {
            return invalid;
        }

        return quantity > int.MaxValue - GetStock(good) ? MarketResult.Overflow : MarketResult.Accepted;
    }

    /// <summary>Would taking this quantity from the stock be accepted? Changes nothing.</summary>
    public MarketResult CheckRemoveStock(GoodDefinition good, int quantity)
    {
        MarketResult invalid = CheckRequest(good, quantity);
        if (invalid != MarketResult.Accepted)
        {
            return invalid;
        }

        return quantity > GetStock(good) ? MarketResult.InsufficientStock : MarketResult.Accepted;
    }

    /// <summary>Adds units to the stock. Accepted, or the reason why nothing was added.</summary>
    public MarketResult TryAddStock(GoodDefinition good, int quantity)
    {
        MarketResult check = CheckAddStock(good, quantity);
        if (check != MarketResult.Accepted)
        {
            return check;
        }

        stocks[good] = GetStock(good) + quantity;
        Changed();
        return MarketResult.Accepted;
    }

    /// <summary>Takes units from the stock. Accepted, or the reason why nothing was taken.</summary>
    public MarketResult TryRemoveStock(GoodDefinition good, int quantity)
    {
        MarketResult check = CheckRemoveStock(good, quantity);
        if (check != MarketResult.Accepted)
        {
            return check;
        }

        stocks[good] = GetStock(good) - quantity;
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

            if (!IsKnown(entry.good))
            {
                Debug.LogWarning("Market '" + TownName + "': the good '" + entry.good.name + "' is not in the catalog.", this);
                problems++;
            }
        }

        return problems;
    }

    private bool IsKnown(GoodDefinition good)
    {
        return catalog.TryGetGood(good.Id, out GoodDefinition known) && known == good;
    }

    private MarketResult CheckRequest(GoodDefinition good, int quantity)
    {
        if (!configured)
        {
            return MarketResult.NotConfigured;
        }

        if (good == null || !IsKnown(good))
        {
            return MarketResult.UnknownGood;
        }

        return quantity > 0 ? MarketResult.Accepted : MarketResult.InvalidQuantity;
    }
}

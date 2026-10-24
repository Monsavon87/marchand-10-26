using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Makes the markets live with the campaign calendar. Every day that begins (the clock raises DayStarted, one by one
/// even when a jump covers several), each market gets one day of production, consumption and return toward its
/// target (see MarketDayRules). The calendar only moves while the world's time runs, so nothing changes while the
/// world is frozen, paused, held by a window or in a fight. A good outside a town's profile has no production,
/// consumption or target: what was sold to the town slowly drains away. The change of each good is applied in one
/// step, so the stock never goes negative.
/// </summary>
public class EconomyTicker : MonoBehaviour
{
    [SerializeField] private CampaignClock clock;
    [SerializeField] private EconomySettings settings;

    private readonly List<IEconomyModifier> modifiers = new List<IEconomyModifier>();

    /// <summary>Raised once the markets have lived the day, with its number.</summary>
    public event Action<int> DayApplied = delegate { };

    private void Start()
    {
        if (clock == null || settings == null)
        {
            Debug.LogError("EconomyTicker: the clock or the settings are not assigned.", this);
            enabled = false;
            return;
        }

        settings.Validate();
    }

    private void OnEnable()
    {
        if (clock != null)
        {
            clock.DayStarted += OnDayStarted;
        }
    }

    private void OnDisable()
    {
        if (clock != null)
        {
            clock.DayStarted -= OnDayStarted;
        }
    }

    /// <summary>Extension point: from now on this modifier shapes each day's production and consumption.</summary>
    public void AddModifier(IEconomyModifier modifier)
    {
        if (modifier != null && !modifiers.Contains(modifier))
        {
            modifiers.Add(modifier);
        }
    }

    public void RemoveModifier(IEconomyModifier modifier)
    {
        modifiers.Remove(modifier);
    }

    private void OnDayStarted(int day)
    {
        IReadOnlyList<Market> markets = Market.All;
        for (int m = 0; m < markets.Count; m++)
        {
            ApplyDay(markets[m], day);
        }

        DayApplied(day);
    }

    /// <summary>
    /// What the stock of this good in this market will be when the next day has been lived (events included). Changes
    /// nothing: it lets the trade service tell where a price is heading.
    /// </summary>
    public int ForecastStock(Market market, GoodDefinition good)
    {
        return NextStockOf(market, good, clock.Day + 1);
    }

    private int NextStockOf(Market market, GoodDefinition good, int day)
    {
        int target = 0;
        int production = 0;
        int consumption = 0;
        if (market.Definition.TryGetEntry(good, out GoodStockProfile entry))
        {
            target = entry.targetStock;
            production = entry.productionPerDay;
            consumption = entry.consumptionPerDay;
        }

        for (int i = 0; i < modifiers.Count; i++)
        {
            production = modifiers[i].AdjustProduction(market, good, day, production);
            consumption = modifiers[i].AdjustConsumption(market, good, day, consumption);
        }

        return MarketDayRules.NextStock(market.GetStock(good), target, production, consumption, settings.ReversionPercent);
    }

    private void ApplyDay(Market market, int day)
    {
        IReadOnlyList<GoodDefinition> goods = market.AllGoods;
        for (int g = 0; g < goods.Count; g++)
        {
            GoodDefinition good = goods[g];
            int stock = market.GetStock(good);
            int next = NextStockOf(market, good, day);
            if (next == stock)
            {
                continue;
            }

            MarketResult result = next > stock ? market.TryAddStock(good, next - stock) : market.TryRemoveStock(good, stock - next);
            if (result != MarketResult.Accepted)
            {
                Debug.LogError("EconomyTicker: day " + day + ", " + market.TownName + ", " + good.Id + ": the stock could not go from " + stock + " to " + next + " (" + result + ").", market);
            }
        }
    }
}

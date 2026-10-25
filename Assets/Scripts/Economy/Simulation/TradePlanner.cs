using System;
using System.Collections.Generic;

/// <summary>One trip the merchant has decided on: what to buy here, where to sell it, and what it expects to earn.</summary>
public readonly struct TradePlan
{
    public TradePlan(Market from, int toIndex, Market to, GoodDefinition good, int quantity, int cost, int revenue, float minutes)
    {
        From = from;
        ToIndex = toIndex;
        To = to;
        Good = good;
        Quantity = quantity;
        Cost = cost;
        Revenue = revenue;
        Minutes = minutes;
    }

    public Market From { get; }
    public int ToIndex { get; }
    public Market To { get; }
    public GoodDefinition Good { get; }
    public int Quantity { get; }
    public int Cost { get; }
    public int Revenue { get; }
    public float Minutes { get; }

    public int Profit => Revenue - Cost;
    public bool IsValid => Good != null && Quantity > 0;
}

/// <summary>
/// The brain of the simulated merchant: from a town, it judges every good carried to every other town and picks the
/// trip that earns the most per day of travel. The quantity is the one that earns the most, within what the trade
/// service says can be bought (the town's stock, the room in the cargo, the money). Prices come from the same
/// pricing rules as the game, unit by unit; with Foresight the destination's stock is the one it will have on arrival.
/// It changes nothing: the trip is carried out by the simulator through the trade service.
/// </summary>
public class TradePlanner
{
    private const float MinutesPerDay = 1440f;

    private readonly TradeService service;
    private readonly IPricingRules rules;
    private readonly int reversionPercent;
    private readonly TravelTimeTable table;
    private readonly IReadOnlyList<Market> markets;
    private readonly IReadOnlyList<GoodDefinition> goods;

    public TradePlanner(TradeService service, IPricingRules rules, int reversionPercent, TravelTimeTable table,
        IReadOnlyList<Market> markets, IReadOnlyList<GoodDefinition> goods)
    {
        this.service = service;
        this.rules = rules;
        this.reversionPercent = reversionPercent;
        this.table = table;
        this.markets = markets;
        this.goods = goods;
    }

    /// <summary>
    /// The best trip from this town (invalid when no trip earns anything). 'startMinutes' is the calendar now, and
    /// each judged route is noted in the result. 'budget' is the most the purchase may cost (the money the merchant
    /// accepts to spend: what it has, minus what it keeps for the wages).
    /// </summary>
    public TradePlan Best(int hereIndex, SimulationStrategy strategy, double startMinutes, ScenarioResult result, long budget)
    {
        Market here = markets[hereIndex];
        TradePlan best = default;
        double bestRate = 0.0;
        for (int g = 0; g < goods.Count; g++)
        {
            GoodDefinition good = goods[g];
            int maxBuy = service.MaxQuantity(TradeKind.Buy, here, good, out _);
            if (maxBuy <= 0 || budget <= 0)
            {
                // Nothing to buy here (no stock, or no money for a single unit): the route is not judged.
                continue;
            }

            for (int d = 0; d < markets.Count; d++)
            {
                // The other town must buy the good (it is in its profile).
                if (d == hereIndex || !markets[d].IsInProfile(good))
                {
                    continue;
                }

                float minutes = table.Minutes(hereIndex, d);
                if (float.IsInfinity(minutes))
                {
                    continue;
                }

                TradePlan plan = Judge(here, markets[d], d, good, maxBuy, minutes, strategy, startMinutes, budget);
                NoteRoute(result, here, markets[d], good, plan);
                double rate = plan.IsValid ? plan.Profit / (double)Math.Max(1f, minutes) : 0.0;
                if (rate > bestRate)
                {
                    bestRate = rate;
                    best = plan;
                }
            }
        }

        return best;
    }

    private TradePlan Judge(Market from, Market to, int toIndex, GoodDefinition good, int maxBuy, float minutes,
        SimulationStrategy strategy, double startMinutes, long budget)
    {
        int stockHere = from.GetStock(good);
        int targetHere = from.GetTarget(good);
        int stockThere = strategy == SimulationStrategy.Foresight ? StockOnArrival(to, good, startMinutes, minutes) : to.GetStock(good);
        int targetThere = to.GetTarget(good);

        long cost = 0;
        long revenue = 0;
        long bestProfit = 0;
        int bestQuantity = 0;
        long bestCost = 0;
        long bestRevenue = 0;
        for (int q = 1; q <= maxBuy; q++)
        {
            cost += rules.BuyUnitPrice(good, stockHere - (q - 1), targetHere);
            if (cost > budget)
            {
                break;
            }

            revenue += rules.SellUnitPrice(good, stockThere + (q - 1), targetThere);
            long profit = revenue - cost;
            if (profit > bestProfit)
            {
                bestProfit = profit;
                bestQuantity = q;
                bestCost = cost;
                bestRevenue = revenue;
            }
        }

        if (bestQuantity == 0 || bestRevenue > int.MaxValue)
        {
            return default;
        }

        return new TradePlan(from, toIndex, to, good, bestQuantity, (int)bestCost, (int)bestRevenue, minutes);
    }

    // The stock of a town when the caravan arrives: one day of production, use and return to target for each
    // midnight crossed on the way (the same rule as the economy ticker).
    private int StockOnArrival(Market market, GoodDefinition good, double startMinutes, float travelMinutes)
    {
        int midnights = (int)Math.Floor((startMinutes + travelMinutes) / MinutesPerDay) - (int)Math.Floor(startMinutes / MinutesPerDay);
        // Outside the town's profile there is no target: what was sold to the town drains away (as in the ticker).
        int target = 0;
        int production = 0;
        int consumption = 0;
        if (market.Definition.TryGetEntry(good, out GoodStockProfile entry))
        {
            target = entry.targetStock;
            production = entry.productionPerDay;
            consumption = entry.consumptionPerDay;
        }

        int stock = market.GetStock(good);
        for (int i = 0; i < midnights; i++)
        {
            stock = MarketDayRules.NextStock(stock, target, production, consumption, reversionPercent);
        }

        return stock;
    }

    private static void NoteRoute(ScenarioResult result, Market from, Market to, GoodDefinition good, TradePlan plan)
    {
        string key = from.TownName + ">" + to.TownName + ">" + good.Id;
        if (!result.Routes.TryGetValue(key, out RouteStats stats))
        {
            stats = new RouteStats { From = from.TownName, To = to.TownName, Good = good };
            result.Routes.Add(key, stats);
        }

        stats.Evaluations++;
        if (plan.IsValid)
        {
            stats.Profitable++;
            stats.BestExpectedProfit = Math.Max(stats.BestExpectedProfit, plan.Profit);
        }
    }
}

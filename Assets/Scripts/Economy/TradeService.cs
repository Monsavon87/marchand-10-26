using System;
using UnityEngine;

/// <summary>
/// The one way to buy from a town and sell to it, and the one place the interface asks about it. A trade is
/// all-or-nothing: every condition (the town's stock, the caravan's money, the cargo's mass and volume, what the
/// player owns) is checked BEFORE anything changes, so a refused trade changes nothing and raises nothing; then the
/// three changes are applied, and if one of them fails anyway (a bug) the ones already done are undone. Only a trade
/// that went through raises Traded.
/// The player buys at the price of the units one after the other (each costs more than the one before), and a town
/// buys every good. The reading methods (UnitPrice, Preview, MaxQuantity, GetTrend, AverageCost, UnitProfit) change nothing and give the
/// interface every figure it shows, so the interface holds no rule of its own. The service does not check that the
/// caravan is in the town: whoever shows the market does (the town windows). It holds no text: see TradeMessages.
/// </summary>
public class TradeService : MonoBehaviour
{
    [SerializeField] private Wallet wallet;
    [SerializeField] private Inventory inventory;
    [SerializeField] private PricingSettings pricingSettings;
    [Tooltip("Gives the game day written in each trade.")]
    [SerializeField] private CampaignClock clock;
    [Tooltip("Tells where a price is heading (the stock of tomorrow).")]
    [SerializeField] private EconomyTicker ticker;

    private IPricingRules rules;
    private bool configured;

    /// <summary>Raised after a trade went through, with what it came to.</summary>
    public event Action<TradeRecord> Traded = delegate { };

    private void Awake()
    {
        if (wallet == null || inventory == null || pricingSettings == null || clock == null || ticker == null)
        {
            Debug.LogError("TradeService: a serialized reference is not assigned.", this);
            return;
        }

        rules = new PricingRules(pricingSettings);
        configured = true;
    }

    /// <summary>Buys 'quantity' units of a good from the town.</summary>
    public TradeResult Buy(Market market, GoodDefinition good, int quantity)
    {
        TradeStatus status = CheckBuy(market, good, quantity, out int total);
        return status == TradeStatus.Done ? ApplyBuy(market, good, quantity, total) : Refused(TradeKind.Buy, good, quantity, status);
    }

    /// <summary>Sells 'quantity' units of a good to the town.</summary>
    public TradeResult Sell(Market market, GoodDefinition good, int quantity)
    {
        TradeStatus status = CheckSell(market, good, quantity, out int total);
        return status == TradeStatus.Done ? ApplySell(market, good, quantity, total) : Refused(TradeKind.Sell, good, quantity, status);
    }

    /// <summary>
    /// Buys as many units as possible: what the town has, what fits in the caravan and what the money pays for,
    /// whichever is least. The result says which of them limited it.
    /// </summary>
    public TradeResult BuyMax(Market market, GoodDefinition good)
    {
        int quantity = MaxQuantity(TradeKind.Buy, market, good, out TradeStatus limit);
        if (quantity <= 0)
        {
            return Refused(TradeKind.Buy, good, 0, limit);
        }

        TradeResult result = Buy(market, good, quantity);
        return result.Success ? result.WithLimit(limit) : result;
    }

    /// <summary>Sells everything the player has of a good (up to the most the pricing handles at once).</summary>
    public TradeResult SellMax(Market market, GoodDefinition good)
    {
        int quantity = MaxQuantity(TradeKind.Sell, market, good, out TradeStatus limit);
        if (quantity <= 0)
        {
            return Refused(TradeKind.Sell, good, 0, limit);
        }

        TradeResult result = Sell(market, good, quantity);
        return result.Success ? result.WithLimit(limit) : result;
    }

    /// <summary>What the next unit costs the player (Buy) or brings (Sell) in this town; 0 when there is none to buy.</summary>
    public int UnitPrice(TradeKind kind, Market market, GoodDefinition good)
    {
        if (!configured || market == null || good == null)
        {
            return 0;
        }

        // A town buys only the goods of its profile: no selling price for the others.
        if (kind == TradeKind.Sell && !market.IsInProfile(good))
        {
            return 0;
        }

        int stock = market.GetStock(good);
        int target = market.GetTarget(good);
        return kind == TradeKind.Buy ? rules.BuyUnitPrice(good, stock, target) : rules.SellUnitPrice(good, stock, target);
    }

    /// <summary>
    /// Where the price of this good in this town is heading if the player does this: tomorrow's price compared to
    /// today's, once 'quantity' units have been bought (the town's stock goes down) or sold (it goes up) and the market
    /// has lived a day. Rounded to a percent; a fixed-price good stays Stable. Not known when the town has fewer units
    /// than a purchase would take. Changes nothing.
    /// </summary>
    public TrendReading GetTrend(Market market, GoodDefinition good, TradeKind kind, int quantity)
    {
        if (!configured || market == null || good == null || quantity <= 0)
        {
            return TrendReading.Unknown;
        }

        int stock = market.GetStock(good);
        if ((kind == TradeKind.Buy && quantity > stock) || (kind == TradeKind.Sell && !market.IsInProfile(good)))
        {
            return TrendReading.Unknown;
        }

        if (good.StablePrice)
        {
            return new TrendReading(PriceTrend.Stable, 0, true);
        }

        int target = market.GetTarget(good);
        int stockAfter = (int)Math.Min((long)stock + (kind == TradeKind.Buy ? -quantity : quantity), int.MaxValue);
        int today = rules.MidPrice(good, stock, target);
        int tomorrow = rules.MidPrice(good, ticker.ForecastStock(market, good, stockAfter), target);
        int percent = (int)Math.Round((tomorrow - today) * 100.0 / today);
        PriceTrend trend = percent > 0 ? PriceTrend.Rising : percent < 0 ? PriceTrend.Falling : PriceTrend.Stable;
        return new TrendReading(trend, percent, true);
    }

    /// <summary>
    /// What one unit of this good carried cost the player, at the average of what the units carried cost, rounded
    /// (0 when none is carried, or when it cost nothing). Changes nothing.
    /// </summary>
    public int AverageCost(GoodDefinition good)
    {
        int owned = configured && good != null ? inventory.GetQuantity(good) : 0;
        return owned > 0 ? (int)Math.Round((double)inventory.GetTotalCost(good) / owned) : 0;
    }

    /// <summary>
    /// What selling one unit of this good here would earn over what it cost (negative: a loss); 0 when none is carried.
    /// The reading of a resale: it holds no rule, the price and the cost come from the pricing and the cargo. Changes
    /// nothing.
    /// </summary>
    public int UnitProfit(Market market, GoodDefinition good)
    {
        if (!configured || market == null || good == null || inventory.GetQuantity(good) <= 0 || !market.IsInProfile(good))
        {
            return 0;
        }

        return UnitPrice(TradeKind.Sell, market, good) - AverageCost(good);
    }

    /// <summary>
    /// What a trade would come to, without doing it: whether it would go through or why not, the total, and the
    /// money and cargo room left afterwards. Changes nothing.
    /// </summary>
    public TradePreview Preview(TradeKind kind, Market market, GoodDefinition good, int quantity)
    {
        int total;
        TradeStatus status = kind == TradeKind.Buy ? CheckBuy(market, good, quantity, out total) : CheckSell(market, good, quantity, out total);
        if (!configured || market == null || good == null)
        {
            return new TradePreview(status, kind, good, quantity, 0, 0, 0f, 0f, 0, 0);
        }

        int balance = wallet.Balance;
        float massLeft = inventory.Capacity.MaxMass - inventory.TotalMass;
        float volumeLeft = inventory.Capacity.MaxVolume - inventory.TotalVolume;
        int available = kind == TradeKind.Buy ? market.GetStock(good) : inventory.GetQuantity(good);
        int missing = status == TradeStatus.NotEnoughMoney ? total - balance : 0;

        // The figures "after the trade" are what it would give when it goes through or falls short only of money or
        // room; for any other refusal nothing would change.
        bool shows = status == TradeStatus.Done || status == TradeStatus.NotEnoughMoney
            || status == TradeStatus.OverMass || status == TradeStatus.OverVolume;
        if (!shows)
        {
            return new TradePreview(status, kind, good, quantity, 0, balance, massLeft, volumeLeft, available, 0);
        }

        int sign = kind == TradeKind.Buy ? 1 : -1;
        int cost = kind == TradeKind.Sell ? (int)Math.Min(inventory.CostOf(good, quantity), int.MaxValue) : 0;
        return new TradePreview(status, kind, good, quantity, total, balance - sign * total,
            massLeft - sign * quantity * good.MassPerUnit, volumeLeft - sign * quantity * good.VolumePerUnit, available, missing, cost);
    }

    /// <summary>
    /// The most units of a good that can be bought (Buy: what the town has, what fits in the cargo and what the money
    /// pays for, whichever is least) or sold (Sell: what the player has). 0 when none; 'limit' then says why, and
    /// otherwise what stopped it from going higher (Done when nothing did). Changes nothing.
    /// </summary>
    public int MaxQuantity(TradeKind kind, Market market, GoodDefinition good, out TradeStatus limit)
    {
        limit = CheckReady(market, good);
        if (limit != TradeStatus.Done)
        {
            return 0;
        }

        return kind == TradeKind.Buy ? MaxBuy(market, good, out limit) : MaxSell(market, good, out limit);
    }

    private int MaxBuy(Market market, GoodDefinition good, out TradeStatus limit)
    {
        int stock = market.GetStock(good);
        int room = inventory.MaxAddable(good);
        int cap = Math.Min(Math.Min(stock, room), PricingRules.MaxQuantity);
        if (cap <= 0)
        {
            // Nothing can be bought: a single unit tells exactly why (no stock, no money, or no room in mass or volume).
            TradeStatus why = CheckBuy(market, good, 1, out _);
            limit = why == TradeStatus.Done ? TradeStatus.InternalError : why;
            return 0;
        }

        // The cost grows with the quantity: find the largest one the money pays for.
        int target = market.GetTarget(good);
        int low = 0;
        int high = cap;
        while (low < high)
        {
            int middle = (low + high + 1) / 2;
            bool affordable = rules.TryQuoteBuy(good, stock, target, middle, out int cost) == PriceQuoteResult.Accepted
                && wallet.CanAfford(cost);
            if (affordable)
            {
                low = middle;
            }
            else
            {
                high = middle - 1;
            }
        }

        if (low == 0)
        {
            limit = TradeStatus.NotEnoughMoney;
            return 0;
        }

        limit = low < cap ? TradeStatus.NotEnoughMoney : WhatLimitedBuy(good, low, stock, room);
        return low;
    }

    private int MaxSell(Market market, GoodDefinition good, out TradeStatus limit)
    {
        if (!market.IsInProfile(good))
        {
            limit = TradeStatus.NotBoughtHere;
            return 0;
        }

        int owned = inventory.GetQuantity(good);
        if (owned <= 0)
        {
            limit = TradeStatus.NotEnoughGoods;
            return 0;
        }

        int quantity = Math.Min(owned, PricingRules.MaxQuantity);
        limit = quantity < owned ? TradeStatus.TooLarge : TradeStatus.Done;
        return quantity;
    }

    // What stopped a "maximum" purchase that the money did not stop. 'stock' and 'room' are the town's stock and the
    // number of units that fit; nothing has been bought yet.
    private TradeStatus WhatLimitedBuy(GoodDefinition good, int quantity, int stock, int room)
    {
        if (quantity >= PricingRules.MaxQuantity && quantity < Math.Min(stock, room))
        {
            return TradeStatus.TooLarge;
        }

        if (stock <= room)
        {
            return TradeStatus.NotEnoughStock;
        }

        // The cargo is what runs out: one more unit tells whether the mass or the volume.
        return CapacityStatus(inventory.CheckAdd(good, quantity + 1));
    }

    private TradeStatus CheckReady(Market market, GoodDefinition good)
    {
        if (!configured || market == null)
        {
            return TradeStatus.NotConfigured;
        }

        return good == null ? TradeStatus.UnknownGood : TradeStatus.Done;
    }

    private TradeStatus CheckBuy(Market market, GoodDefinition good, int quantity, out int total)
    {
        total = 0;
        TradeStatus ready = CheckReady(market, good);
        if (ready != TradeStatus.Done)
        {
            return ready;
        }

        if (quantity <= 0)
        {
            return TradeStatus.InvalidQuantity;
        }

        switch (market.CheckRemoveStock(good, quantity))
        {
            case MarketResult.Accepted:
                break;
            case MarketResult.InsufficientStock:
                return TradeStatus.NotEnoughStock;
            case MarketResult.UnknownGood:
                return TradeStatus.UnknownGood;
            default:
                return TradeStatus.NotConfigured;
        }

        PriceQuoteResult quote = rules.TryQuoteBuy(good, market.GetStock(good), market.GetTarget(good), quantity, out total);
        if (quote != PriceQuoteResult.Accepted)
        {
            return TradeStatus.TooLarge;
        }

        if (!wallet.CanAfford(total))
        {
            return TradeStatus.NotEnoughMoney;
        }

        return CapacityStatus(inventory.CheckAdd(good, quantity));
    }

    private TradeStatus CheckSell(Market market, GoodDefinition good, int quantity, out int total)
    {
        total = 0;
        TradeStatus ready = CheckReady(market, good);
        if (ready != TradeStatus.Done)
        {
            return ready;
        }

        if (quantity <= 0)
        {
            return TradeStatus.InvalidQuantity;
        }

        if (inventory.GetQuantity(good) < quantity)
        {
            return TradeStatus.NotEnoughGoods;
        }

        // A town buys only what it makes or uses (its profile).
        if (!market.IsInProfile(good))
        {
            return TradeStatus.NotBoughtHere;
        }

        switch (market.CheckAddStock(good, quantity))
        {
            case MarketResult.Accepted:
                break;
            case MarketResult.Overflow:
                return TradeStatus.TooLarge;
            case MarketResult.UnknownGood:
                return TradeStatus.UnknownGood;
            default:
                return TradeStatus.NotConfigured;
        }

        PriceQuoteResult quote = rules.TryQuoteSell(good, market.GetStock(good), market.GetTarget(good), quantity, out total);
        if (quote != PriceQuoteResult.Accepted)
        {
            return TradeStatus.TooLarge;
        }

        return wallet.CheckDeposit(total) == WalletResult.Accepted ? TradeStatus.Done : TradeStatus.TooLarge;
    }

    private static TradeStatus CapacityStatus(InventoryResult result)
    {
        switch (result)
        {
            case InventoryResult.Accepted: return TradeStatus.Done;
            case InventoryResult.MassExceeded: return TradeStatus.OverMass;
            case InventoryResult.VolumeExceeded: return TradeStatus.OverVolume;
            case InventoryResult.UnknownGood: return TradeStatus.UnknownGood;
            case InventoryResult.InvalidQuantity: return TradeStatus.InvalidQuantity;
            default: return TradeStatus.NotConfigured;
        }
    }

    // Money, then the town's stock, then the cargo; a later failure undoes the earlier steps.
    private TradeResult ApplyBuy(Market market, GoodDefinition good, int quantity, int total)
    {
        if (wallet.TryWithdraw(total) != WalletResult.Accepted)
        {
            return Failed(TradeKind.Buy, good, quantity, "the payment was refused");
        }

        if (market.TryRemoveStock(good, quantity) != MarketResult.Accepted)
        {
            Undo(wallet.TryDeposit(total) == WalletResult.Accepted);
            return Failed(TradeKind.Buy, good, quantity, "the town's stock could not be taken");
        }

        if (inventory.TryAdd(good, quantity, total) != InventoryResult.Accepted)
        {
            bool undone = market.TryAddStock(good, quantity) == MarketResult.Accepted;
            undone &= wallet.TryDeposit(total) == WalletResult.Accepted;
            Undo(undone);
            return Failed(TradeKind.Buy, good, quantity, "the cargo could not be added");
        }

        return Completed(TradeKind.Buy, market, good, quantity, total);
    }

    // The cargo, then the town's stock, then the money; a later failure undoes the earlier steps.
    private TradeResult ApplySell(Market market, GoodDefinition good, int quantity, int total)
    {
        if (inventory.TryRemove(good, quantity, out long removedCost) != InventoryResult.Accepted)
        {
            return Failed(TradeKind.Sell, good, quantity, "the cargo could not be taken");
        }

        if (market.TryAddStock(good, quantity) != MarketResult.Accepted)
        {
            Undo(inventory.TryAdd(good, quantity, (int)removedCost) == InventoryResult.Accepted);
            return Failed(TradeKind.Sell, good, quantity, "the town's stock could not be added");
        }

        if (wallet.TryDeposit(total) != WalletResult.Accepted)
        {
            bool undone = market.TryRemoveStock(good, quantity) == MarketResult.Accepted;
            undone &= inventory.TryAdd(good, quantity, (int)removedCost) == InventoryResult.Accepted;
            Undo(undone);
            return Failed(TradeKind.Sell, good, quantity, "the payment could not be deposited");
        }

        return Completed(TradeKind.Sell, market, good, quantity, total, total - (int)removedCost);
    }

    private TradeResult Completed(TradeKind kind, Market market, GoodDefinition good, int quantity, int total, int profit = 0)
    {
        Traded(new TradeRecord(clock.Day, kind, market.TownName, good, quantity, total, profit));
        return new TradeResult(TradeStatus.Done, kind, good, quantity, quantity, total, TradeStatus.Done);
    }

    private static TradeResult Refused(TradeKind kind, GoodDefinition good, int requested, TradeStatus status)
    {
        return new TradeResult(status, kind, good, requested, 0, 0, TradeStatus.Done);
    }

    private TradeResult Failed(TradeKind kind, GoodDefinition good, int requested, string what)
    {
        Debug.LogError("TradeService: a step failed although every condition was checked (" + what + "): the trade was undone.", this);
        return Refused(kind, good, requested, TradeStatus.InternalError);
    }

    private void Undo(bool succeeded)
    {
        if (!succeeded)
        {
            Debug.LogError("TradeService: a trade could not be undone: money, stock and cargo may be out of step.", this);
        }
    }
}

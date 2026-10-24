using System;
using UnityEngine;

/// <summary>
/// The one way to buy from a town and sell to it. A trade is all-or-nothing: every condition (the town's stock, the
/// caravan's money, the cargo's mass and volume, what the player owns) is checked BEFORE anything changes, so a
/// refused trade changes nothing and raises nothing; then the three changes are applied, and if one of them fails
/// anyway (a bug) the ones already done are undone. Only a trade that went through raises Traded.
/// The player buys at the price of the units one after the other (each costs more than the one before), and a town
/// buys every good. The service does not check that the caravan is in the town: whoever calls it does (the town
/// window, later). It holds no text: see TradeMessages.
/// </summary>
public class TradeService : MonoBehaviour
{
    [SerializeField] private Wallet wallet;
    [SerializeField] private Inventory inventory;
    [SerializeField] private PricingSettings pricingSettings;
    [Tooltip("Gives the game day written in each trade.")]
    [SerializeField] private CampaignClock clock;

    private IPricingRules rules;
    private bool configured;

    /// <summary>Raised after a trade went through, with what it came to.</summary>
    public event Action<TradeRecord> Traded = delegate { };

    private void Awake()
    {
        if (wallet == null || inventory == null || pricingSettings == null || clock == null)
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
        TradeStatus ready = CheckReady(market, good);
        if (ready != TradeStatus.Done)
        {
            return Refused(TradeKind.Buy, good, 0, ready);
        }

        int stock = market.GetStock(good);
        int room = inventory.MaxAddable(good);
        int limit = Math.Min(Math.Min(stock, room), PricingRules.MaxQuantity);
        if (limit <= 0)
        {
            // Nothing can be bought: a single unit tells exactly why (no stock, or no room in mass or volume).
            TradeStatus why = CheckBuy(market, good, 1, out _);
            return Refused(TradeKind.Buy, good, 0, why == TradeStatus.Done ? TradeStatus.InternalError : why);
        }

        // The cost grows with the quantity: find the largest one the money pays for.
        int target = market.GetTarget(good);
        int low = 0;
        int high = limit;
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
            return Refused(TradeKind.Buy, good, 0, TradeStatus.NotEnoughMoney);
        }

        TradeResult result = Buy(market, good, low);
        if (!result.Success)
        {
            return result;
        }

        return result.WithLimit(low < limit ? TradeStatus.NotEnoughMoney : WhatLimitedBuy(good, low, stock, room));
    }

    /// <summary>Sells everything the player has of a good (up to the most the pricing handles at once).</summary>
    public TradeResult SellMax(Market market, GoodDefinition good)
    {
        TradeStatus ready = CheckReady(market, good);
        if (ready != TradeStatus.Done)
        {
            return Refused(TradeKind.Sell, good, 0, ready);
        }

        int owned = inventory.GetQuantity(good);
        if (owned <= 0)
        {
            return Refused(TradeKind.Sell, good, 0, TradeStatus.NotEnoughGoods);
        }

        int quantity = Math.Min(owned, PricingRules.MaxQuantity);
        TradeResult result = Sell(market, good, quantity);
        return result.Success && quantity < owned ? result.WithLimit(TradeStatus.TooLarge) : result;
    }

    // What stopped a "maximum" purchase that was not stopped by the money.
    // Called once the purchase is done: 'stock' and 'room' are what they were before it.
    private TradeStatus WhatLimitedBuy(GoodDefinition good, int bought, int stock, int room)
    {
        if (bought >= PricingRules.MaxQuantity && bought < Math.Min(stock, room))
        {
            return TradeStatus.TooLarge;
        }

        if (stock <= room)
        {
            return TradeStatus.NotEnoughStock;
        }

        // The cargo is what ran out: one more unit, on top of what was just bought, tells whether the mass or the volume.
        return CapacityStatus(inventory.CheckAdd(good, 1));
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

        if (inventory.TryAdd(good, quantity) != InventoryResult.Accepted)
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
        if (inventory.TryRemove(good, quantity) != InventoryResult.Accepted)
        {
            return Failed(TradeKind.Sell, good, quantity, "the cargo could not be taken");
        }

        if (market.TryAddStock(good, quantity) != MarketResult.Accepted)
        {
            Undo(inventory.TryAdd(good, quantity) == InventoryResult.Accepted);
            return Failed(TradeKind.Sell, good, quantity, "the town's stock could not be added");
        }

        if (wallet.TryDeposit(total) != WalletResult.Accepted)
        {
            bool undone = market.TryRemoveStock(good, quantity) == MarketResult.Accepted;
            undone &= inventory.TryAdd(good, quantity) == InventoryResult.Accepted;
            Undo(undone);
            return Failed(TradeKind.Sell, good, quantity, "the payment could not be deposited");
        }

        return Completed(TradeKind.Sell, market, good, quantity, total);
    }

    private TradeResult Completed(TradeKind kind, Market market, GoodDefinition good, int quantity, int total)
    {
        Traded(new TradeRecord(clock.Day, kind, market.TownName, good, quantity, total));
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

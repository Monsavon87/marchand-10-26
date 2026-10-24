using System;
using UnityEngine;

/// <summary>
/// The default pricing rules. Price at a stock level = base price x factor(stock / target), the factor following
/// the curve of the settings and kept between its two limits, rounded, at least 1 U.V.; a good with a fixed price
/// ignores the factor. The margin is then added (rounded up) when the player buys and taken (rounded down) when
/// the player sells.
/// Each unit is priced at the LOWER stock level of its move: a unit bought takes the stock from k+1 to k and is
/// priced at k; a unit sold takes it from k to k+1 and is priced at k. So a unit bought and sold back is priced on
/// the same level both times, and a round trip loses at least the margin, however steep the curve is.
/// Plain data in, plain data out: it knows nothing about markets, inventories or the interface.
/// </summary>
public class PricingRules : IPricingRules
{
    /// <summary>The most units priced in one go (the total is summed unit by unit).</summary>
    public const int MaxQuantity = 100000;

    // Float arithmetic can land a hair above or under a whole number (240 x 1.05): this slack absorbs it.
    private const double Epsilon = 1e-9;

    private readonly PricingSettings settings;

    public PricingRules(PricingSettings settings)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public float PriceFactor(GoodDefinition good, int stock, int target)
    {
        if (good.StablePrice)
        {
            return 1f;
        }

        // A target under 1 is a data mistake that TownDefinition already warns about; it is treated as 1 here.
        float ratio = (float)Math.Max(0, stock) / Math.Max(1, target);
        float factor = settings.FactorCurve.Evaluate(ratio);
        return Mathf.Clamp(factor, settings.MinFactor, settings.MaxFactor);
    }

    public int MidPrice(GoodDefinition good, int stock, int target)
    {
        return Math.Max(1, Mathf.RoundToInt(good.BasePrice * PriceFactor(good, stock, target)));
    }

    public int BuyUnitPrice(GoodDefinition good, int stock, int target)
    {
        if (stock <= 0)
        {
            return 0;
        }

        // The unit leaves the stock: it takes it from 'stock' to 'stock - 1', and is priced at the lower level.
        int mid = MidPrice(good, stock - 1, target);
        return (int)Math.Max(1.0, Math.Ceiling(mid * (1.0 + settings.HalfSpread) - Epsilon));
    }

    public int SellUnitPrice(GoodDefinition good, int stock, int target)
    {
        int mid = MidPrice(good, Math.Max(0, stock), target);
        return (int)Math.Max(1.0, Math.Floor(mid * (1.0 - settings.HalfSpread) + Epsilon));
    }

    public PriceQuoteResult TryQuoteBuy(GoodDefinition good, int stock, int target, int quantity, out int total)
    {
        total = 0;
        if (good == null)
        {
            return PriceQuoteResult.InvalidGood;
        }

        if (quantity <= 0 || quantity > MaxQuantity)
        {
            return PriceQuoteResult.InvalidQuantity;
        }

        if (quantity > stock)
        {
            return PriceQuoteResult.InsufficientStock;
        }

        long sum = 0;
        for (int i = 0; i < quantity; i++)
        {
            sum += BuyUnitPrice(good, stock - i, target);
        }

        return Finish(sum, out total);
    }

    public PriceQuoteResult TryQuoteSell(GoodDefinition good, int stock, int target, int quantity, out int total)
    {
        total = 0;
        if (good == null)
        {
            return PriceQuoteResult.InvalidGood;
        }

        if (quantity <= 0 || quantity > MaxQuantity)
        {
            return PriceQuoteResult.InvalidQuantity;
        }

        long sum = 0;
        for (int i = 0; i < quantity; i++)
        {
            sum += SellUnitPrice(good, (int)Math.Min((long)Math.Max(0, stock) + i, int.MaxValue), target);
        }

        return Finish(sum, out total);
    }

    private static PriceQuoteResult Finish(long sum, out int total)
    {
        if (sum > int.MaxValue)
        {
            total = 0;
            return PriceQuoteResult.Overflow;
        }

        total = (int)sum;
        return PriceQuoteResult.Accepted;
    }
}

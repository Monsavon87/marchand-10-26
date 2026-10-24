/// <summary>
/// How a town prices its goods, from its stock and its target stock. Whoever needs a price (the trade service, the
/// debug views) uses this interface, so the rules can be replaced by others without touching them. All prices are
/// whole numbers of U.V., at least 1. "Buy" is the player buying from the town, "sell" the player selling to it.
/// </summary>
public interface IPricingRules
{
    /// <summary>The factor applied to the base price at this stock level (1 for a good with a fixed price).</summary>
    float PriceFactor(GoodDefinition good, int stock, int target);

    /// <summary>The price of the good at this stock level, before the margin.</summary>
    int MidPrice(GoodDefinition good, int stock, int target);

    /// <summary>What the next unit costs the player when the town has 'stock' (0 when the town has none).</summary>
    int BuyUnitPrice(GoodDefinition good, int stock, int target);

    /// <summary>What the town pays the player for the next unit when it has 'stock'.</summary>
    int SellUnitPrice(GoodDefinition good, int stock, int target);

    /// <summary>The total the player pays for 'quantity' units: each unit costs more than the one before.</summary>
    PriceQuoteResult TryQuoteBuy(GoodDefinition good, int stock, int target, int quantity, out int total);

    /// <summary>The total the town pays for 'quantity' units: each unit brings less than the one before.</summary>
    PriceQuoteResult TryQuoteSell(GoodDefinition good, int stock, int target, int quantity, out int total);
}

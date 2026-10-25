/// <summary>The outcome of a trade. Anything but Done means nothing changed: not the money, the stock nor the cargo.</summary>
public enum TradeStatus
{
    Done,
    /// <summary>The quantity is 0 or negative.</summary>
    InvalidQuantity,
    /// <summary>No town, or the trade service is missing a reference.</summary>
    NotConfigured,
    /// <summary>The good is missing or is not one of the catalog's.</summary>
    UnknownGood,
    /// <summary>Buying: the town has fewer units than asked.</summary>
    NotEnoughStock,
    /// <summary>Buying: the player does not have the money.</summary>
    NotEnoughMoney,
    /// <summary>Buying: the units would be too heavy for the caravan.</summary>
    OverMass,
    /// <summary>Buying: the units would take too much room in the caravan.</summary>
    OverVolume,
    /// <summary>Selling: the player does not have that many units.</summary>
    NotEnoughGoods,
    /// <summary>Selling: the town does not buy this good (it is not in its profile: the town neither makes nor uses it).</summary>
    NotBoughtHere,
    /// <summary>The quantity or the amount is too large to be handled (more than the pricing or a whole number allows).</summary>
    TooLarge,
    /// <summary>A step failed although every condition had been checked: what was done has been undone (a bug).</summary>
    InternalError
}

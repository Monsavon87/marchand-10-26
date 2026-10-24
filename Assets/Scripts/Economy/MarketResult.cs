/// <summary>The outcome of an operation on a Market. Anything but Accepted means the stock did not change.</summary>
public enum MarketResult
{
    Accepted,
    /// <summary>The quantity is 0 or negative.</summary>
    InvalidQuantity,
    /// <summary>The good is missing, or the town does not trade it.</summary>
    NotTraded,
    /// <summary>Removing more than the town has.</summary>
    InsufficientStock,
    /// <summary>Adding would go past the largest whole number the stock can hold.</summary>
    Overflow,
    /// <summary>The market has no profile, town or catalog assigned.</summary>
    NotConfigured
}

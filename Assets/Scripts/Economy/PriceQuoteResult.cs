/// <summary>The outcome of asking for the price of a quantity. Anything but Accepted means no total was given.</summary>
public enum PriceQuoteResult
{
    Accepted,
    /// <summary>The quantity is 0 or negative, or more than the rules agree to price at once.</summary>
    InvalidQuantity,
    /// <summary>The good is missing.</summary>
    InvalidGood,
    /// <summary>Buying more units than the town has in stock.</summary>
    InsufficientStock,
    /// <summary>The total is more than the largest whole number.</summary>
    Overflow
}

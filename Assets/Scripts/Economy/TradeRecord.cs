/// <summary>One trade that went through, as the journal keeps it.</summary>
public readonly struct TradeRecord
{
    public TradeRecord(int day, TradeKind kind, string townName, GoodDefinition good, int quantity, int amount, int profit = 0)
    {
        Day = day;
        Kind = kind;
        TownName = townName;
        Good = good;
        Quantity = quantity;
        Amount = amount;
        Profit = profit;
    }

    /// <summary>The game day of the campaign clock.</summary>
    public int Day { get; }
    public TradeKind Kind { get; }
    public string TownName { get; }
    public GoodDefinition Good { get; }
    public int Quantity { get; }
    /// <summary>Total paid or received, in U.V.</summary>
    public int Amount { get; }
    /// <summary>A sale: what it earned over what its units cost (negative: a loss). A purchase: 0.</summary>
    public int Profit { get; }
}

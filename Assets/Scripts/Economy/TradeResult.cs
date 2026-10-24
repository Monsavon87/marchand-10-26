/// <summary>
/// What a trade came to: the status (Done, or the precise reason of the refusal), the quantity really traded and the
/// amount in U.V. (both 0 when refused). For a "maximum" trade, Limit says what stopped it from going further
/// (Done when nothing did).
/// </summary>
public readonly struct TradeResult
{
    public TradeResult(TradeStatus status, TradeKind kind, GoodDefinition good, int requested, int quantity, int amount, TradeStatus limit)
    {
        Status = status;
        Kind = kind;
        Good = good;
        RequestedQuantity = requested;
        Quantity = quantity;
        Amount = amount;
        Limit = limit;
    }

    public TradeStatus Status { get; }
    public TradeKind Kind { get; }
    public GoodDefinition Good { get; }
    /// <summary>What was asked (for a "maximum" trade, what the maximum worked out to).</summary>
    public int RequestedQuantity { get; }
    /// <summary>Units really bought or sold.</summary>
    public int Quantity { get; }
    /// <summary>Total paid (buying) or received (selling), in U.V.</summary>
    public int Amount { get; }
    /// <summary>For a "maximum" trade: what limited the quantity. Done when nothing did.</summary>
    public TradeStatus Limit { get; }

    public bool Success => Status == TradeStatus.Done;

    public TradeResult WithLimit(TradeStatus limit)
    {
        return new TradeResult(Status, Kind, Good, RequestedQuantity, Quantity, Amount, limit);
    }
}

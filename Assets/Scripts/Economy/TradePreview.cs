/// <summary>
/// What a trade WOULD come to, asked of the trade service without doing anything: whether it would go through (Status
/// is Done) or why not, the total in U.V., and what the money and the cargo would be after it. When the trade is
/// refused only for lack of money or of room, the figures are still the ones it would give, so the player can see by
/// how much it falls short; for any other refusal they are the current ones. Everything the interface shows about a
/// trade comes from here: it works out nothing itself.
/// </summary>
public readonly struct TradePreview
{
    public TradePreview(TradeStatus status, TradeKind kind, GoodDefinition good, int quantity, int amount,
        int balanceAfter, float massLeftAfter, float volumeLeftAfter, int available, int moneyMissing, int cost = 0)
    {
        Status = status;
        Kind = kind;
        Good = good;
        Quantity = quantity;
        Amount = amount;
        BalanceAfter = balanceAfter;
        MassLeftAfter = massLeftAfter;
        VolumeLeftAfter = volumeLeftAfter;
        Available = available;
        MoneyMissing = moneyMissing;
        Cost = cost;
    }

    public TradeStatus Status { get; }
    public TradeKind Kind { get; }
    public GoodDefinition Good { get; }
    /// <summary>The quantity asked about.</summary>
    public int Quantity { get; }
    /// <summary>Total paid (buying) or received (selling), in U.V.; 0 when it cannot be worked out.</summary>
    public int Amount { get; }
    public int BalanceAfter { get; }
    /// <summary>Room left in the cargo, in kg, after the trade. Negative when it would not fit.</summary>
    public float MassLeftAfter { get; }
    /// <summary>Room left in the cargo, in liters, after the trade. Negative when it would not fit.</summary>
    public float VolumeLeftAfter { get; }
    /// <summary>Buying: units the town has. Selling: units the player has.</summary>
    public int Available { get; }
    /// <summary>U.V. the player lacks to afford it (0 when the money is enough).</summary>
    public int MoneyMissing { get; }

    /// <summary>
    /// Selling: what the units sold cost the player (at the average of what the units carried cost; 0 for goods that cost
    /// nothing, such as loot, or when it cannot be worked out). Buying: 0.
    /// </summary>
    public int Cost { get; }
    /// <summary>Selling: what the sale earns over what its units cost (negative: a loss). Buying: 0.</summary>
    public int Profit => Kind == TradeKind.Sell && Success ? Amount - Cost : 0;
    /// <summary>The profit as a share of the cost, rounded, in percent; 0 when the units cost nothing.</summary>
    public int ProfitPercent => Cost > 0 ? (int)System.Math.Round(Profit * 100.0 / Cost) : 0;

    public bool Success => Status == TradeStatus.Done;
}

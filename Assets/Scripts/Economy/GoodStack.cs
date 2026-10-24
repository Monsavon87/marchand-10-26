/// <summary>A quantity of one good (loot, a cargo part). Plain data.</summary>
public readonly struct GoodStack
{
    public GoodStack(GoodDefinition good, int quantity)
    {
        Good = good;
        Quantity = quantity;
    }

    public GoodDefinition Good { get; }
    public int Quantity { get; }

    /// <summary>What the stack is worth at the goods' base price, in U.V.</summary>
    public long BaseValue => Good != null ? (long)Good.BasePrice * Quantity : 0L;
}

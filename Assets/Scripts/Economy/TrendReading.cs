/// <summary>
/// Where the price of a good in a town is heading if the player does something: tomorrow's price compared to today's,
/// once the market has lived the day after the action (buying lowers the stock, so the price tends to rise; selling
/// raises it, so the price tends to fall). Known is false when the action could not be done at all (more units than the
/// town has). Plain data, made by the trade service.
/// </summary>
public readonly struct TrendReading
{
    public TrendReading(PriceTrend trend, int percent, bool known)
    {
        Trend = trend;
        Percent = percent;
        Known = known;
    }

    public PriceTrend Trend { get; }
    /// <summary>The change of the price from today to tomorrow, rounded, in percent (negative: a fall).</summary>
    public int Percent { get; }
    public bool Known { get; }

    public static TrendReading Unknown => new TrendReading(PriceTrend.Stable, 0, false);
}

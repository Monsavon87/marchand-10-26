/// <summary>
/// The extension point for one-off events (a famine, a festival, a caravan that arrives): a modifier changes what a
/// market produces or consumes of a good on a given day. The economy ticker accepts a list of them; nothing
/// registers one yet, so the list stays empty and the markets follow their profiles alone.
/// </summary>
public interface IEconomyModifier
{
    /// <summary>The production of this good in this market on this day, from the profile's 'production'.</summary>
    int AdjustProduction(Market market, GoodDefinition good, int day, int production);

    /// <summary>The consumption of this good in this market on this day, from the profile's 'consumption'.</summary>
    int AdjustConsumption(Market market, GoodDefinition good, int day, int consumption);
}

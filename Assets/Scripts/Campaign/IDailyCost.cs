/// <summary>
/// A cost the caravan pays each world day it travels (see PartyPayroll): the wages of the group are the first. It is the
/// extension point for future costs (tolls, upkeep): a new source registers itself on the payroll and is paid with the
/// rest, without touching it. A source only says what it costs; it never touches the wallet.
/// </summary>
public interface IDailyCost
{
    /// <summary>What the cost is called, in the words shown to the player.</summary>
    string Label { get; }

    /// <summary>What it costs on this day, in U.V. (0 or more) for this group.</summary>
    int CostFor(int day, PartyRoster party);
}

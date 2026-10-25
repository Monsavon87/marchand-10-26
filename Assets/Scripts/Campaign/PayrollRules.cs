using System;

/// <summary>
/// What one day of wages does to the money and the debt: the debt is paid first, then the day's cost, as far as the
/// money goes; what cannot be paid becomes the new debt. Nothing is charged during the free days at the start. Whole
/// numbers only, no scene: used by the game (PartyPayroll) and by the economy simulator, so both follow the same rule.
/// </summary>
public static class PayrollRules
{
    /// <summary>True when this day is charged (day 1 is the first day; the free days are not charged).</summary>
    public static bool IsCharged(int day, int graceDays)
    {
        return day > graceDays;
    }

    /// <summary>
    /// Settles one day: 'paid' is what leaves the wallet (never more than the balance), 'debtAfter' what is still owed.
    /// </summary>
    public static void Settle(int debtBefore, int cost, int balance, out int paid, out int debtAfter)
    {
        long due = (long)Math.Max(0, debtBefore) + Math.Max(0, cost);
        paid = (int)Math.Min(due, Math.Max(0, balance));
        debtAfter = (int)Math.Min(due - paid, int.MaxValue);
    }
}

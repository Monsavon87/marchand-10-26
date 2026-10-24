using System;

/// <summary>
/// What a day of natural healing does to one unit, and what the hospital asks. Whole numbers only, and no scene,
/// roster or clock: it only turns numbers into numbers.
/// </summary>
public static class HealingRules
{
    /// <summary>
    /// The hit points after one day of natural healing: the unit regains 'percentPerDay' of its maximum (rounded up, at
    /// least 1 hit point), but never goes above 'capPercent' of its maximum (rounded down, at least 1). A unit already at or
    /// above the ceiling keeps what it has: natural healing never takes anything away, and never heals past the ceiling.
    /// </summary>
    public static int NextHealth(int current, int max, int percentPerDay, int capPercent)
    {
        if (max <= 0)
        {
            return Math.Max(current, 0);
        }

        int ceiling = Math.Max(1, Math.Min(max, (int)((long)max * Math.Max(0, capPercent) / 100)));
        if (current >= ceiling)
        {
            return current;
        }

        int gain = Math.Max(1, (int)(((long)max * Math.Max(0, percentPerDay) + 99) / 100));
        return Math.Min(ceiling, current + gain);
    }

    /// <summary>The hit points a unit is missing.</summary>
    public static int Missing(int current, int max)
    {
        return Math.Max(0, max - current);
    }

    /// <summary>What the hospital asks to give back 'hitPoints' (0 when there is nothing to give back).</summary>
    public static long Price(int hitPoints, int pricePerHitPoint)
    {
        return (long)Math.Max(0, hitPoints) * Math.Max(0, pricePerHitPoint);
    }
}

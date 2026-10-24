using System;

/// <summary>
/// What one day does to the stock of ONE good in ONE market, in this order: the town produces, the town consumes
/// (the stock never goes under 0), then the stock comes back part of the way toward its target. Whole numbers only,
/// and no scene, market or clock: it only turns numbers into numbers.
/// </summary>
public static class MarketDayRules
{
    /// <summary>
    /// The stock after one day. 'reversionPercent' is the share of the gap to the target that is closed (1 to 100):
    /// the stock moves by that share of the gap rounded up, so at least 1 unit, and never past the target.
    /// </summary>
    public static int NextStock(int stock, int target, int production, int consumption, int reversionPercent)
    {
        long produced = (long)Math.Max(0, stock) + Math.Max(0, production);
        long left = Math.Max(0L, produced - Math.Max(0, consumption));
        int afterUse = (int)Math.Min(left, int.MaxValue);

        long gap = (long)Math.Max(0, target) - afterUse;
        if (gap == 0)
        {
            return afterUse;
        }

        long size = Math.Abs(gap);
        long percent = Math.Max(1, Math.Min(100, reversionPercent));
        long step = Math.Min(size, (size * percent + 99) / 100);
        return (int)(gap > 0 ? afterUse + step : afterUse - step);
    }
}

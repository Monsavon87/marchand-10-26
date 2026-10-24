using System.Globalization;

/// <summary>
/// How an amount of U.V. is written on screen: thousands separated by a plain space (15 000). A plain space, not
/// the narrow no-break one of the French culture, which the default font may not draw.
/// </summary>
public static class MoneyFormat
{
    public const string Unit = "U.V.";

    /// <summary>"15 000".</summary>
    public static string Number(int amount)
    {
        return amount.ToString("N0", CultureInfo.InvariantCulture).Replace(',', ' ');
    }

    /// <summary>"15 000 U.V.".</summary>
    public static string WithUnit(int amount)
    {
        return Number(amount) + " " + Unit;
    }
}

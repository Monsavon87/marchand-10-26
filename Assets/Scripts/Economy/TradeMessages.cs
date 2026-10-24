using System.Globalization;

/// <summary>The sentences shown to the player for each trade status, so the trade service itself holds no text.</summary>
public static class TradeMessages
{
    public static string Describe(TradeStatus status)
    {
        switch (status)
        {
            case TradeStatus.Done: return "Échange réalisé.";
            case TradeStatus.InvalidQuantity: return "Quantité invalide (il faut au moins 1).";
            case TradeStatus.NotConfigured: return "Commerce indisponible (ville ou service non configuré).";
            case TradeStatus.UnknownGood: return "Marchandise inconnue.";
            case TradeStatus.NotEnoughStock: return "Pas assez de stock en ville.";
            case TradeStatus.NotEnoughMoney: return "Pas assez d'U.V.";
            case TradeStatus.OverMass: return "Trop lourd : la capacité en masse de la caravane serait dépassée.";
            case TradeStatus.OverVolume: return "Trop volumineux : la capacité en volume de la caravane serait dépassée.";
            case TradeStatus.NotEnoughGoods: return "Vous n'avez pas assez de cette marchandise.";
            case TradeStatus.TooLarge: return "Quantité ou montant trop grand.";
            case TradeStatus.InternalError: return "Échange annulé : erreur interne.";
            default: return status.ToString();
        }
    }

    /// <summary>
    /// The sentence for a trade that would not go through, with the figures that explain it (what the town has, what
    /// is missing). For one that would, a short confirmation.
    /// </summary>
    public static string Describe(TradePreview preview)
    {
        switch (preview.Status)
        {
            case TradeStatus.NotEnoughStock:
                return preview.Available == 0 ? "La ville n'en a pas en stock." : "La ville n'en a que " + preview.Available + ".";
            case TradeStatus.NotEnoughMoney:
                return "Pas assez d'U.V. : il vous en manque " + MoneyFormat.Number(preview.MoneyMissing) + ".";
            case TradeStatus.OverMass:
                return "Trop lourd : il manque " + (-preview.MassLeftAfter).ToString("0.#", CultureInfo.InvariantCulture) + " kg de capacité.";
            case TradeStatus.OverVolume:
                return "Trop volumineux : il manque " + (-preview.VolumeLeftAfter).ToString("0.#", CultureInfo.InvariantCulture) + " L de capacité.";
            case TradeStatus.NotEnoughGoods:
                return preview.Available == 0 ? "Vous n'en possédez pas." : "Vous n'en possédez que " + preview.Available + ".";
            case TradeStatus.Done:
                return "Possible.";
            default:
                return Describe(preview.Status);
        }
    }

    /// <summary>The sentence for a trade that went through.</summary>
    public static string Describe(TradeRecord record)
    {
        return (record.Kind == TradeKind.Buy ? "Acheté : " : "Vendu : ") + record.Quantity + " " + record.Good.DisplayName
            + (record.Kind == TradeKind.Buy ? " pour " : " contre ") + MoneyFormat.WithUnit(record.Amount);
    }

    /// <summary>The sentence for a whole result: the refusal reason, or what was done (with what limited a "maximum").</summary>
    public static string Describe(TradeResult result)
    {
        if (!result.Success)
        {
            return Describe(result.Status);
        }

        string what = (result.Kind == TradeKind.Buy ? "Acheté : " : "Vendu : ") + result.Quantity + " " + result.Good.DisplayName
            + " pour " + MoneyFormat.WithUnit(result.Amount);
        return result.Limit == TradeStatus.Done ? what : what + " Limité par : " + Describe(result.Limit);
    }
}

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

    /// <summary>The sentence for a whole result: the refusal reason, or what was done (with what limited a "maximum").</summary>
    public static string Describe(TradeResult result)
    {
        if (!result.Success)
        {
            return Describe(result.Status);
        }

        string what = (result.Kind == TradeKind.Buy ? "Acheté : " : "Vendu : ") + result.Quantity + " " + result.Good.DisplayName
            + " pour " + MoneyFormat.WithUnit(result.Amount) + ".";
        return result.Limit == TradeStatus.Done ? what : what + " Limité par : " + Describe(result.Limit);
    }
}

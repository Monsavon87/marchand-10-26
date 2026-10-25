using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

/// <summary>
/// Turns the runs of the economy simulator into the balance report (Markdown, in French): the summary of the
/// scenarios, the travel times, what earns money, how the prices moved, the full loop with fights, the anomalies
/// found and the parameters to adjust. Pure text from data: it touches nothing.
/// </summary>
public static class SimulationReportWriter
{
    private const float MinutesPerDay = 1440f;
    private const double ConcentrationShare = 0.3;

    public static string Build(SimulationSettings settings, PricingSettings pricing, IReadOnlyList<Market> markets,
        IReadOnlyList<GoodDefinition> goods, TravelTimeTable travel, IReadOnlyList<ScenarioResult> scenarios, IReadOnlyList<ScenarioResult> otherSeeds,
        IReadOnlyList<LootTable> bandTables, int recruitPrice)
    {
        ScenarioResult reference = scenarios[1];
        ScenarioResult replay = scenarios[2];
        ScenarioResult combat = scenarios[3];
        var text = new StringBuilder();
        text.AppendLine("# Rapport d'équilibrage économique (étape E9)");
        text.AppendLine();
        text.AppendLine("Simulation sans rendu de **" + settings.Days + " jours**, graine **" + settings.Seed + "**, capital de départ **"
            + Money(settings.StartingCapital) + "**, " + markets.Count + " villes, " + goods.Count + " marchandises. "
            + "Le marchand achète et vend uniquement par `TradeService`, avec le calendrier et les durées de trajet réelles de la carte "
            + "(chemin NavMesh, terrains compris). Ce rapport est reproductible : même graine, même résultat.");
        text.AppendLine();

        AppendSummary(text, scenarios);
        AppendTravel(text, markets, travel);
        AppendEarnings(text, reference);
        AppendPrices(text, reference, goods, pricing);
        AppendLoop(text, combat, reference, otherSeeds);
        AppendStart(text, scenarios[4], scenarios[5], bandTables, recruitPrice);
        List<string> anomalies = FindAnomalies(settings, pricing, scenarios, otherSeeds, markets, replay, reference);
        AppendAnomalies(text, anomalies);
        AppendParameters(text, settings, pricing, reference, anomalies);
        AppendLootPrinciple(text);
        AppendLimits(text);
        return text.ToString();
    }

    private static void AppendSummary(StringBuilder text, IReadOnlyList<ScenarioResult> scenarios)
    {
        text.AppendLine("## 1. Résumé des scénarios");
        text.AppendLine();
        text.AppendLine("| Scénario | Profit net | Profit / jour | Trajets | Profit / trajet | Jours d'attente | Combats (V/D) | Fin |");
        text.AppendLine("|---|---:|---:|---:|---:|---:|---:|---|");
        for (int i = 0; i < scenarios.Count; i++)
        {
            ScenarioResult s = scenarios[i];
            int trips = s.Trips.Count;
            text.AppendLine("| " + s.Name + " | " + Money(s.NetProfit) + " | " + Money((long)s.ProfitPerDay) + " | " + trips + " | "
                + (trips > 0 ? Money(s.NetProfit / trips) : "-") + " | " + s.IdleDays + " | " + s.Victories + " / " + s.Defeats + " | " + s.EndReason + " |");
        }

        text.AppendLine();
    }

    private static void AppendTravel(StringBuilder text, IReadOnlyList<Market> markets, TravelTimeTable travel)
    {
        text.AppendLine("## 2. Durées de trajet (jours de calendrier, distance en mètres)");
        text.AppendLine();
        text.Append("| De \\ Vers |");
        for (int j = 0; j < markets.Count; j++)
        {
            text.Append(' ').Append(markets[j].TownName).Append(" |");
        }

        text.AppendLine();
        text.Append("|---|");
        for (int j = 0; j < markets.Count; j++)
        {
            text.Append("---:|");
        }

        text.AppendLine();
        for (int i = 0; i < markets.Count; i++)
        {
            text.Append("| " + markets[i].TownName + " (" + markets[i].Definition.ProfileName + ") |");
            for (int j = 0; j < markets.Count; j++)
            {
                text.Append(i == j ? " - |" : " " + (travel.Minutes(i, j) / MinutesPerDay).ToString("0.0", CultureInfo.InvariantCulture)
                    + " j (" + travel.Meters(i, j).ToString("0", CultureInfo.InvariantCulture) + " m) |");
            }

            text.AppendLine();
        }

        text.AppendLine();
    }

    private static void AppendEarnings(StringBuilder text, ScenarioResult reference)
    {
        text.AppendLine("## 3. Ce qui rapporte (scénario B, sans combat)");
        text.AppendLine();
        text.AppendLine("Par marchandise transportée :");
        text.AppendLine();
        text.AppendLine("| Marchandise | Trajets | Unités | Coût | Recette | Profit | Rendement moyen |");
        text.AppendLine("|---|---:|---:|---:|---:|---:|---:|");
        foreach (IGrouping<GoodDefinition, TripRecord> group in reference.Trips.GroupBy(t => t.Good).OrderByDescending(g => g.Sum(t => (long)t.Profit)))
        {
            long cost = group.Sum(t => (long)t.Cost);
            long revenue = group.Sum(t => (long)t.Revenue);
            text.AppendLine("| " + group.Key.DisplayName + " | " + group.Count() + " | " + group.Sum(t => t.Quantity) + " | " + Money(cost) + " | "
                + Money(revenue) + " | " + Money(revenue - cost) + " | " + Percent(cost > 0 ? (revenue - cost) / (double)cost : 0.0) + " |");
        }

        text.AppendLine();
        text.AppendLine("Par route (ville de départ > ville d'arrivée) :");
        text.AppendLine();
        text.AppendLine("| Route | Trajets | Profit | Profit / jour de trajet |");
        text.AppendLine("|---|---:|---:|---:|");
        foreach (IGrouping<string, TripRecord> group in reference.Trips.GroupBy(t => t.From + " > " + t.To).OrderByDescending(g => g.Sum(t => (long)t.Profit)))
        {
            double days = group.Sum(t => t.Minutes) / MinutesPerDay;
            text.AppendLine("| " + group.Key + " | " + group.Count() + " | " + Money(group.Sum(t => (long)t.Profit)) + " | "
                + Money((long)(group.Sum(t => (long)t.Profit) / Math.Max(0.01, days))) + " |");
        }

        text.AppendLine();
        text.AppendLine("Trajets (les 12 premiers et les 4 derniers) :");
        text.AppendLine();
        text.AppendLine("| Jour | Trajet | Marchandise | Quantité | Coût | Recette | Profit | Profit prévu |");
        text.AppendLine("|---:|---|---|---:|---:|---:|---:|---:|");
        for (int i = 0; i < reference.Trips.Count; i++)
        {
            if (i >= 12 && i < reference.Trips.Count - 4)
            {
                if (i == 12)
                {
                    text.AppendLine("| ... | | | | | | | |");
                }

                continue;
            }

            TripRecord t = reference.Trips[i];
            text.AppendLine("| " + t.StartDay + " | " + t.From + " > " + t.To + " | " + t.Good.DisplayName + " | " + t.Quantity + " | " + Money(t.Cost)
                + " | " + Money(t.Revenue) + " | " + Money(t.Profit) + " | " + Money(t.ExpectedProfit) + " |");
        }

        text.AppendLine();
    }

    private static void AppendPrices(StringBuilder text, ScenarioResult reference, IReadOnlyList<GoodDefinition> goods, PricingSettings pricing)
    {
        text.AppendLine("## 4. Évolution des prix (scénario B)");
        text.AppendLine();
        text.AppendLine("Prix médian (sans écart acheteur/vendeur) : moyenne, minimum et maximum sur la durée, par ville. `profil` = la ville produit ou consomme la marchandise ; "
            + "`hors profil` = elle l'achète seulement, son stock tend vers 0. Entre parenthèses : part des jours au prix plancher (×" + pricing.MinFactor.ToString("0.##", CultureInfo.InvariantCulture)
            + ") / plafond (×" + pricing.MaxFactor.ToString("0.##", CultureInfo.InvariantCulture) + ").");
        text.AppendLine();
        for (int g = 0; g < goods.Count; g++)
        {
            GoodDefinition good = goods[g];
            text.AppendLine("**" + good.DisplayName + "** (prix de base " + Money(good.BasePrice) + (good.StablePrice ? ", cours fixe" : "") + ")");
            text.AppendLine();
            text.AppendLine("| Ville | Profil | Prix moyen | Min | Max | Stock min-max | Plancher / plafond | Jours à stock 0 |");
            text.AppendLine("|---|---|---:|---:|---:|---:|---:|---:|");
            foreach (PriceSeries s in reference.Series.Where(x => x.Good == good))
            {
                text.AppendLine("| " + s.Town + " | " + (s.InProfile ? "profil" : "hors profil") + " | " + Money((long)s.MidMean) + " | " + Money(s.MidMin) + " | " + Money(s.MidMax)
                    + " | " + s.StockMin + "-" + s.StockMax + " | " + Percent(Share(s.DaysAtFloor, s.Samples)) + " / " + Percent(Share(s.DaysAtCeiling, s.Samples))
                    + " | " + Percent(Share(s.DaysZeroStock, s.Samples)) + " |");
            }

            text.AppendLine();
        }
    }

    private static void AppendLoop(StringBuilder text, ScenarioResult combat, ScenarioResult reference, IReadOnlyList<ScenarioResult> otherSeeds)
    {
        text.AppendLine("## 5. La boucle complète : acheter, voyager, combattre, revenir, vendre (scénario C)");
        text.AppendLine();
        text.AppendLine("Les combats sont tirés au sort (pas d'arène) : chance de rencontre par jour de voyage, chance de victoire réduite quand le groupe est amoindri, "
            + "butin de chaque bande de la carte (tiré dans sa table, U.V. et marchandises ; une bande battue revient après le délai des réglages de retour avec un nouveau butin ; les marchandises qui ne rentrent pas dans la cargaison sont perdues, les plus chères entrent d'abord), unités perdues rachetées à la ville suivante au prix de la moins chère des recrues.");
        text.AppendLine();
        text.AppendLine("- Profit net : **" + Money(combat.NetProfit) + "** en " + combat.DaysElapsed + " jours (sans combat : " + Money(reference.NetProfit) + ").");
        text.AppendLine("- Combats : " + combat.Victories + " victoire(s), " + combat.Defeats + " défaite(s) ; unités perdues : " + combat.UnitsLost
            + " ; valeur du butin ramassé : " + Money(combat.LootTotal) + " (perdu faute de place : " + Money(combat.LootLostValue) + ") ; coût des remplacements : " + Money(combat.ReplacementCost) + ".");
        text.AppendLine("- Salaires payés : " + Money(combat.WagesPaid) + " (sans combat : " + Money(reference.WagesPaid) + ") ; dette restante : " + Money(combat.EndingDebt)
            + " (déjà retirée du profit net).");
        text.AppendLine("- Fin : " + combat.EndReason);
        text.AppendLine();
        var all = new List<ScenarioResult> { combat };
        all.AddRange(otherSeeds);
        ScenarioResult detail = all.FirstOrDefault(s => s.Defeats == 0) ?? combat;
        text.AppendLine("Trajets avec un combat (graine " + detail.Seed + (detail == combat ? "" : ", la première partie menée jusqu'au bout") + ") : "
            + detail.Victories + " victoire(s), " + detail.UnitsLost + " unité(s) perdue(s), butin " + Money(detail.LootTotal) + ", remplacements " + Money(detail.ReplacementCost) + ".");
        text.AppendLine();
        text.AppendLine("| Jour | Trajet | Résultat | Valeur du butin | Profit (vente du butin comprise) |");
        text.AppendLine("|---:|---|---|---:|---:|");
        foreach (TripRecord t in detail.Trips.Where(x => x.Fight.Length > 0))
        {
            text.AppendLine("| " + t.StartDay + " | " + t.From + " > " + t.To + " (" + t.Good.DisplayName + ") | " + t.Fight + " | " + Money(t.Loot) + " | " + Money(t.Profit) + " |");
        }

        text.AppendLine();
        if (otherSeeds.Count > 0)
        {
            text.AppendLine("Effet de la chance (même scénario, " + (otherSeeds.Count + 1) + " graines) :");
            text.AppendLine();
            text.AppendLine("| Graine | Profit net | Jours | Victoires | Défaites | Unités perdues | Valeur du butin | Butin perdu | Remplacements | Fin |");
            text.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|");
            foreach (ScenarioResult s in all)
            {
                text.AppendLine("| " + s.Seed + " | " + Money(s.NetProfit) + " | " + s.DaysElapsed + " | " + s.Victories + " | " + s.Defeats + " | " + s.UnitsLost + " | "
                    + Money(s.LootTotal) + " | " + Money(s.LootLostValue) + " | " + Money(s.ReplacementCost) + " | " + s.EndReason + " |");
            }

            int lost = all.Count(s => s.Defeats > 0);
            text.AppendLine();
            text.AppendLine("Parties perdues en combat : " + lost + " sur " + all.Count + ". Profit net : minimum " + Money(all.Min(s => (long)s.NetProfit))
                + ", moyenne " + Money((long)all.Average(s => (double)s.NetProfit)) + ", maximum " + Money(all.Max(s => (long)s.NetProfit)) + ".");
            text.AppendLine();
        }
    }

    private static void AppendStart(StringBuilder text, ScenarioResult start, ScenarioResult startWithGoods, IReadOnlyList<LootTable> bandTables, int recruitPrice)
    {
        text.AppendLine("## 5 bis. Le départ de la partie manuelle");
        text.AppendLine();
        text.AppendLine("La caravane démarre avec **0 U.V.** : sans argent on ne peut rien acheter, la première somme vient du butin d'une bande (une partie en U.V., le reste en marchandises à revendre). "
            + "Butin des bandes de la carte (valeur totale tirée dans la table de la bande, U.V. et marchandises au prix de base) : " + (bandTables.Count > 0 ? string.Join(", ", bandTables.OrderBy(b => b.MinValue).Select(b => Money(b.MinValue) + " à " + Money(b.MaxValue))) : "aucune bande") + ". "
            + "La recrue la moins chère coûte " + Money(recruitPrice) + ".");
        text.AppendLine();
        text.AppendLine(DescribeStart("Scénario E : un marchand prévoyant qui part avec la seule part en U.V. du plus petit butin", start));
        text.AppendLine();
        text.AppendLine(DescribeStart("Scénario E2 : le même, en comptant aussi les marchandises de ce butin comme de l'argent", startWithGoods));
        text.AppendLine();
    }

    private static string DescribeStart(string intro, ScenarioResult run)
    {
        int doubling = -1;
        for (int d = 0; d < run.DailyBalance.Count && run.StartingCapital > 0; d++)
        {
            if (run.DailyBalance[d] >= 2 * run.StartingCapital)
            {
                doubling = d;
                break;
            }
        }

        return intro + " (" + Money(run.StartingCapital) + ") finit à " + Money(run.EndingBalance) + " en " + run.DaysElapsed + " jours (" + run.Trips.Count + " trajets)"
            + (doubling >= 0 ? " ; il double son capital au jour " + doubling + "." : " ; il ne double pas son capital sur la durée.") + " " + run.EndReason;
    }

    private static void AppendLimits(StringBuilder text)
    {
        text.AppendLine("## 9. Limites de cette simulation");
        text.AppendLine();
        text.AppendLine("- Le marchand simulé est un optimiseur à courte vue (le meilleur profit par jour du prochain trajet) : un humain peut faire mieux (enchaîner des routes) ou moins bien.");
        text.AppendLine("- Les combats sont un modèle tiré au sort (chance de rencontre, chance de victoire, pertes) dont les valeurs sont des hypothèses de `SimulationSettings`, pas des mesures : "
            + "le taux de défaites du scénario C en dépend entièrement. Les vrais combats, eux, ne sont pas joués.");
        text.AppendLine("- Le profit d'un trajet avec combat comprend la vente des marchandises ramassées (elles se mélangent à la cargaison) : ces trajets sont exclus du contrôle de rendement par trajet.");
        text.AppendLine("- Les blessures et leur soin (régénération, hôpital) ne sont pas modélisés : un combat gagné ne coûte que les unités perdues.");
        text.AppendLine("- Les bandes sont supposées immobiles et rencontrées au hasard sur la route ; les patrouilles réelles ne sont pas modélisées.");
        text.AppendLine("- Les salaires sont ceux du vrai groupe de la scène (sa composition réelle, pas les 4 unités du modèle de combat) et sont prélevés par la vraie paie à chaque minuit : "
            + "le marchand simulé ne les prévoit pas, il dépense tout son argent en marchandises. Le calendrier ne revient jamais en arrière : seul le premier scénario profite des jours offerts.");
        text.AppendLine("- Le temps passé en ville est nul (comme dans le jeu) ; le recrutement se paie directement, sans passer par la fenêtre de recrutement.");
        text.AppendLine("- Les durées de trajet sont celles du chemin NavMesh à vitesse de base ralentie par les terrains traversés ; un détour décidé par le joueur n'est pas simulé.");
        text.AppendLine();
    }

    private static List<string> FindAnomalies(SimulationSettings settings, PricingSettings pricing, IReadOnlyList<ScenarioResult> scenarios,
        IReadOnlyList<ScenarioResult> otherSeeds, IReadOnlyList<Market> markets, ScenarioResult replay, ScenarioResult reference)
    {
        var found = new List<string>();
        var every = new List<ScenarioResult>(scenarios);
        every.AddRange(otherSeeds);

        // Determinism of the dice.
        if (reference.Fingerprint() != replay.Fingerprint())
        {
            found.Add("**Simulation non reproductible** : le scénario B rejoué avec la même graine donne une autre suite de trajets.");
        }

        // A profit that keeps growing.
        foreach (ScenarioResult s in scenarios.Take(2))
        {
            string growth = GrowthWithoutLimit(s, settings.GrowthWindowDays);
            if (growth.Length > 0)
            {
                found.Add("**Profit qui ne se stabilise pas** (scénario " + s.Name + ") : " + growth);
            }
        }

        // A single trip that returns too much.
        TripRecord best = null;
        double bestReturn = 0.0;
        foreach (ScenarioResult s in every)
        {
            foreach (TripRecord t in s.Trips.Where(x => x.Cost > 0 && x.Revenue > 0 && x.Fight.Length == 0))
            {
                double r = t.Profit / (double)t.Cost;
                if (r > bestReturn)
                {
                    bestReturn = r;
                    best = t;
                }
            }
        }

        if (best != null && bestReturn > settings.MaxTripReturn)
        {
            found.Add("**Rendement d'un trajet trop élevé** : " + Percent(bestReturn) + " du coût en un seul trajet (" + best.Good.DisplayName + ", " + best.From + " > " + best.To
                + ", jour " + best.StartDay + "), seuil " + Percent(settings.MaxTripReturn) + ".");
        }

        // A profit that rests on a few trips.
        long totalProfit = reference.Trips.Sum(t => (long)t.Profit);
        long topProfit = reference.Trips.OrderByDescending(t => t.Profit).Take(3).Sum(t => (long)t.Profit);
        if (totalProfit > 0 && topProfit / (double)totalProfit >= ConcentrationShare)
        {
            found.Add("**Profit concentré** : les 3 meilleurs trajets sur " + reference.Trips.Count + " font " + Percent(topProfit / (double)totalProfit)
                + " du profit total (" + Money(topProfit) + " sur " + Money(totalProfit) + "), seuil " + Percent(ConcentrationShare) + " : une seule route écrase les autres.");
        }

        // A prediction that does not match what the service did.
        foreach (ScenarioResult s in every)
        {
            foreach (string failure in s.InvariantFailures.Distinct().Take(5))
            {
                found.Add("**Incohérence** (" + s.Name + ") : " + failure);
            }
        }

        // Prices stuck at a bound, stocks at 0.
        foreach (PriceSeries s in reference.Series)
        {
            if (s.Good.StablePrice)
            {
                continue;
            }

            if (Share(s.DaysAtCeiling, s.Samples) >= settings.StuckShare)
            {
                found.Add((s.InProfile ? "**Prix collé au plafond**" : "Prix au plafond car stock vide (hors profil)") + " : " + s.Good.DisplayName + " à " + s.Town
                    + " (" + Percent(Share(s.DaysAtCeiling, s.Samples)) + " des jours).");
            }

            if (Share(s.DaysAtFloor, s.Samples) >= settings.StuckShare)
            {
                found.Add("**Prix collé au plancher** : " + s.Good.DisplayName + " à " + s.Town + " (" + Percent(Share(s.DaysAtFloor, s.Samples)) + " des jours).");
            }
        }

        foreach (PriceSeries s in reference.Series.Where(x => x.InProfile))
        {
            if (Share(s.DaysZeroStock, s.Samples) >= settings.StuckShare)
            {
                found.Add("**Stock à zéro durablement** : " + s.Good.DisplayName + " à " + s.Town + " (" + Percent(Share(s.DaysZeroStock, s.Samples)) + " des jours).");
            }
        }

        // Routes never worth it.
        foreach (IGrouping<string, RouteStats> pair in reference.Routes.Values.GroupBy(r => r.From + " > " + r.To))
        {
            if (pair.All(r => r.Profitable == 0))
            {
                found.Add("**Route jamais rentable** : " + pair.Key + " (" + pair.Sum(r => r.Evaluations) + " évaluations, aucune marchandise ne rapporte).");
            }
        }

        string never = string.Join(", ", reference.Routes.Values.GroupBy(r => r.Good).Where(g => g.All(r => r.Profitable == 0)).Select(g => g.Key.DisplayName));
        if (never.Length > 0)
        {
            found.Add("Marchandise(s) jamais rentable(s) en transport : " + never + ".");
        }

        double bound = pricing.MaxFactor * (1.0 - pricing.SpreadPercent / 200.0) / (pricing.MinFactor * (1.0 + pricing.SpreadPercent / 200.0)) - 1.0;
        if (bound > settings.MaxTripReturn)
        {
            found.Add("Marge théorique maximale d'une unité (vendre au plafond, acheter au plancher) : " + Percent(bound) + " du prix d'achat, "
                + "au-dessus du seuil de " + Percent(settings.MaxTripReturn) + " : les bornes de prix laissent de la place à de très grosses marges.");
        }

        return found;
    }

    // Describes a profit that rises from slice to slice of 'window' days; empty when it levels off.
    private static string GrowthWithoutLimit(ScenarioResult s, int window)
    {
        var profits = new List<long>();
        for (int start = 0; start + window < s.DailyBalance.Count; start += window)
        {
            profits.Add(s.DailyBalance[start + window] - s.DailyBalance[start]);
        }

        if (profits.Count < 3)
        {
            return string.Empty;
        }

        int n = profits.Count;
        bool growing = profits[n - 1] > profits[n - 2] * 1.1 && profits[n - 2] > profits[n - 3] * 1.1 && profits[n - 3] > 0;
        return growing ? "profit par tranche de " + window + " jours : " + string.Join(", ", profits.Select(p => Money(p))) + " (plus de 10 % de hausse à chaque tranche)." : string.Empty;
    }

    private static void AppendAnomalies(StringBuilder text, List<string> anomalies)
    {
        text.AppendLine("## 6. Anomalies");
        text.AppendLine();
        if (anomalies.Count == 0)
        {
            text.AppendLine("Aucune anomalie détectée.");
        }
        else
        {
            foreach (string anomaly in anomalies)
            {
                text.AppendLine("- " + anomaly);
            }
        }

        text.AppendLine();
    }

    private static void AppendParameters(StringBuilder text, SimulationSettings settings, PricingSettings pricing, ScenarioResult reference, List<string> anomalies)
    {
        text.AppendLine("## 7. Paramètres à ajuster d'après ce rapport");
        text.AppendLine();
        text.AppendLine("Valeurs actuelles : écart acheteur/vendeur " + pricing.SpreadPercent.ToString("0.#", CultureInfo.InvariantCulture) + " %, facteur de prix de ×"
            + pricing.MinFactor.ToString("0.##", CultureInfo.InvariantCulture) + " à ×" + pricing.MaxFactor.ToString("0.##", CultureInfo.InvariantCulture)
            + ", capital de départ simulé " + Money(settings.StartingCapital) + ".");
        text.AppendLine();
        text.AppendLine("| Paramètre | Où | Effet sur le rapport |");
        text.AppendLine("|---|---|---|");
        text.AppendLine("| Facteurs minimum et maximum, courbe de prix | `PricingSettings` | Bornent la marge d'une unité et le rendement d'un trajet. |");
        text.AppendLine("| Écart acheteur/vendeur (`spreadPercent`) | `PricingSettings` | Coût fixe de chaque aller-retour ; plus il est grand, moins les petites routes rapportent. |");
        text.AppendLine("| Retour vers la cible (`reversionPercent`) | `EconomySettings` | Vitesse à laquelle un marché que l'on a vidé ou inondé retrouve son prix : plus il est lent, plus on épuise une route. |");
        text.AppendLine("| Production, consommation et stock cible par marchandise | `TownDefinition` (5 villes) | Fixent quelles routes existent et leur profondeur. |");
        text.AppendLine("| Stock cible par défaut des marchandises hors profil | `TownDefinition.defaultTargetStock` | Fixe à quel point une ville qui n'en a pas paie cher ce qu'on lui apporte. |");
        text.AppendLine("| Masse et volume maximaux de la cargaison | `CargoCapacity_Caravan` | Plafond du profit par trajet : c'est lui qui empêche un profit sans fin. |");
        text.AppendLine("| Prix de base, masse et volume de chaque marchandise | `GoodDefinition` | Valeur transportée par kilo et par litre, donc rentabilité relative. |");
        text.AppendLine("| Minutes par mètre | `CampaignClock` | Durée des trajets, donc profit par jour. |");
        text.AppendLine("| Butin des bandes, prix des recrues | `PartyRoster` (scène), `RecruitmentSettings` | Poids des combats par rapport au commerce. |");
        text.AppendLine();
        text.AppendLine("Pistes déduites des anomalies ci-dessus :");
        text.AppendLine();
        bool any = false;
        foreach (string hint in anomalies.Select(HintFor).Where(h => h.Length > 0).Distinct())
        {
            text.AppendLine("- " + hint);
            any = true;
        }

        if (!any)
        {
            text.AppendLine("- Aucune correction nécessaire d'après ce rapport.");
        }

        text.AppendLine();
    }

    private static string HintFor(string anomaly)
    {
        if (anomaly.Contains("Rendement d'un trajet") || anomaly.Contains("Marge théorique"))
        {
            return "Resserrer les bornes de prix (`minFactor` plus haut, `maxFactor` plus bas) ou élargir l'écart acheteur/vendeur, pour réduire la marge maximale d'un trajet.";
        }

        if (anomaly.Contains("Profit qui ne se stabilise pas"))
        {
            return "Réduire la capacité de la cargaison ou le retour vers la cible des marchés, pour que le profit par trajet se stabilise.";
        }

        if (anomaly.Contains("hors profil") || anomaly.Contains("Profit concentré"))
        {
            return "Les villes paient cher ce qu'elles n'ont pas : relever `defaultTargetStock` des profils (ou borner le prix d'une marchandise hors profil) pour qu'elles ne soient pas l'unique source de marge.";
        }

        if (anomaly.Contains("Stock à zéro"))
        {
            return "Augmenter la production ou baisser la consommation de la marchandise concernée dans la ville signalée.";
        }

        if (anomaly.Contains("Prix collé"))
        {
            return "Revoir la production, la consommation et la cible de la marchandise signalée : son stock reste trop loin de l'équilibre.";
        }

        if (anomaly.Contains("Route jamais rentable") || anomaly.Contains("jamais rentable(s)"))
        {
            return "Rapprocher les profils des villes concernées (une ville qui produit ce que l'autre consomme) ou réduire l'écart acheteur/vendeur.";
        }

        if (anomaly.Contains("Incohérence") || anomaly.Contains("non reproductible"))
        {
            return "Bogue à corriger avant tout équilibrage : voir le détail de l'anomalie.";
        }

        return string.Empty;
    }

    private static void AppendLootPrinciple(StringBuilder text)
    {
        text.AppendLine("## 8. Butin en marchandises (étape F1, en jeu)");
        text.AppendLine();
        text.AppendLine("- Chaque bande a une table de butin (`LootTable`) : une valeur totale entre un minimum et un maximum, répartie au hasard entre les U.V. et des marchandises selon des poids ; tirée une fois à la création de la bande (`BandLootSetup`, `LootRoller`).");
        text.AppendLine("- Les marchandises entrent dans la cargaison à la victoire, les plus chères d'abord, dans la limite de la masse et du volume ; le reste est perdu et dit à l'écran de fin de combat.");
        text.AppendLine("- Elles se vendent ensuite aux prix du marché comme toute cargaison. Le scénario C les simule avec le même tirage et la même règle de place.");
        text.AppendLine("- Garde-fou : la valeur totale d'un butin reste dans l'ordre de grandeur de la table de la bande ; à comparer, dans ce rapport, au profit du commerce.");
        text.AppendLine();
    }

    private static double Share(int count, int total)
    {
        return total > 0 ? count / (double)total : 0.0;
    }

    private static string Percent(double value)
    {
        return (value * 100.0).ToString("0", CultureInfo.InvariantCulture) + " %";
    }

    private static string Money(long amount)
    {
        return amount.ToString("N0", CultureInfo.InvariantCulture).Replace(',', ' ') + " U.V.";
    }
}

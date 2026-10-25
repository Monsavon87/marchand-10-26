using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

/// <summary>
/// Turns the runs of the economy simulator into the balance report (Markdown, in French): the verdict on the goals of
/// the balance, the summary of the scenarios, the travel times, what earns money, how the prices moved, the full loop
/// (fights, loot, care, recruits, wages), the effect of each mechanic, the long runs, the anomalies, the automatic test
/// protocol and the settings to adjust. Pure text from data: it touches nothing.
/// </summary>
public static class SimulationReportWriter
{
    private const float MinutesPerDay = 1440f;
    private const double ConcentrationShare = 0.3;
    private const double OffProfileShare = 0.3;

    private sealed class Check
    {
        public string Name;
        public bool Passed;
        public string Detail;
    }

    public static string Build(SimulationSettings settings, PricingSettings pricing, HealingSettings healing, PayrollSettings payroll,
        IReadOnlyList<Market> markets, IReadOnlyList<GoodDefinition> goods, TravelTimeTable travel, SimulationRun run)
    {
        var text = new StringBuilder();
        text.AppendLine("# Rapport d'équilibrage économique (boucle complète : butin, retour des bandes, soin, recrues, salaires)");
        text.AppendLine();
        text.AppendLine("Simulation sans rendu de **" + settings.Days + " jours** (et " + settings.LongRunDays + " jours pour les parties longues), graine **" + settings.Seed
            + "**, capital de départ **" + Money(settings.StartingCapital) + "**, " + markets.Count + " villes, " + goods.Count + " marchandises. "
            + "Le marchand achète et vend uniquement par `TradeService`, avec le calendrier et les durées de trajet réelles de la carte "
            + "(chemin NavMesh, terrains compris). Son groupe part de la composition réelle de la caravane (" + run.StartingParty + ") ; il est payé chaque jour "
            + "par la règle du jeu (`PayrollRules` : " + payroll.GraceDays + " jours offerts, dette payée en premier), se soigne par la régénération du jeu ("
            + healing.RegenPercentPerDay + " % par jour jusqu'à " + healing.RegenCapPercent + " %) et à l'hôpital (" + Money(healing.PricePerHitPoint) + " par PV). "
            + "Ce rapport est reproductible : même graine, même résultat. **Aucun réglage du jeu n'a été modifié.**");
        text.AppendLine();

        List<string> anomalies = FindAnomalies(settings, pricing, run, markets);
        List<Check> checks = RunChecks(run, payroll);
        AppendVerdict(text, settings, run);
        AppendSummary(text, run.Scenarios);
        AppendTravel(text, markets, travel);
        AppendEarnings(text, run.Reference);
        AppendOffProfile(text, run);
        AppendPrices(text, run.Reference, goods, pricing);
        AppendMeasuredFights(text, run);
        AppendLoop(text, settings, run);
        AppendStart(text, run);
        AppendMechanics(text, run);
        AppendSensitivity(text, settings, run);
        AppendLongRuns(text, settings, run);
        AppendAnomalies(text, anomalies);
        AppendChecks(text, checks);
        AppendParameters(text, settings, pricing, run, anomalies);
        AppendLimits(text, settings);
        return text.ToString();
    }

    // ---- Verdict -------------------------------------------------------------------------------------------------

    private static void AppendVerdict(StringBuilder text, SimulationSettings settings, SimulationRun run)
    {
        List<ScenarioResult> seeds = run.LoopSeeds();
        int positive = seeds.Count(s => s.NetProfit > 0);
        double meanShare = seeds.Average(s => s.LootShare);
        bool profitPositive = run.Loop.NetProfit > 0 && run.Reference.NetProfit > 0 && positive * 2 >= seeds.Count;

        double ratioB = LastToSecondRatio(run.Reference, settings.GrowthWindowDays);
        double ratioC = LastToSecondRatio(run.Loop, settings.GrowthWindowDays);
        double longB = ThirdsRatio(run.ReferenceLong);
        double longC = ThirdsRatio(run.LoopLong);
        bool bounded = ratioB <= settings.BoundedGrowthRatio && (double.IsNaN(ratioC) || ratioC <= settings.BoundedGrowthRatio);
        bool noInfinite = longB <= 1.1 && (double.IsNaN(longC) || longC <= 1.1)
            && GrowthWithoutLimit(run.ReferenceLong, settings.GrowthWindowDays).Length == 0
            && run.Scenarios.All(s => s.EndingBalance < int.MaxValue / 2);
        // On the mean of the seeds: a single run that ends on its first fight says nothing of the weight of the loot.
        bool lootOk = meanShare >= settings.LootShareMin && meanShare <= settings.LootShareMax;

        text.AppendLine("## 0. Verdict");
        text.AppendLine();
        text.AppendLine("| Critère | Mesure | Résultat |");
        text.AppendLine("|---|---|---|");
        text.AppendLine("| Profit positif (après salaires, soins et recrues) | B : " + Money(run.Reference.NetProfit) + " (" + Money((long)run.Reference.ProfitPerDay)
            + " par jour) ; C : " + Money(run.Loop.NetProfit) + " (" + Money((long)run.Loop.ProfitPerDay) + " par jour) ; C positif sur " + positive + " graines sur " + seeds.Count
            + " | " + Verdict(profitPositive) + " |");
        text.AppendLine("| Profit borné (dernière tranche de " + settings.GrowthWindowDays + " jours ≤ " + Ratio(settings.BoundedGrowthRatio) + " × la deuxième) | B : × " + Ratio(ratioB)
            + " ; C : " + (double.IsNaN(ratioC) ? "partie trop courte" : "× " + Ratio(ratioC)) + " | " + Verdict(bounded) + " |");
        text.AppendLine("| Butin qui pèse sans dominer (" + Percent(settings.LootShareMin) + " à " + Percent(settings.LootShareMax) + " des recettes, moyenne des graines) | moyenne des " + seeds.Count
            + " graines : " + Percent(meanShare) + " (de " + Percent(seeds.Min(s => s.LootShare)) + " à " + Percent(seeds.Max(s => s.LootShare)) + ") ; graine " + run.Loop.Seed + " seule : " + LootShareText(run.Loop)
            + " | " + Verdict(lootOk) + " |");
        text.AppendLine("| Aucun profit infini (" + settings.LongRunDays + " jours : dernier tiers ≤ 1,1 × le tiers du milieu, pas de dépassement) | B : × " + Ratio(longB)
            + " ; C : " + (double.IsNaN(longC) ? "partie arrêtée avant la fin" : "× " + Ratio(longC)) + " | " + Verdict(noInfinite) + " |");
        text.AppendLine();
        int survived = seeds.Count(s => s.Defeats == 0);
        text.AppendLine("Survie de la boucle complète : " + survived + " partie(s) sur " + seeds.Count + " vont au bout des " + settings.Days + " jours sans défaite (une défaite met fin à la campagne, comme dans le jeu).");
        text.AppendLine();
    }

    // ---- Summary -------------------------------------------------------------------------------------------------

    private static void AppendSummary(StringBuilder text, IReadOnlyList<ScenarioResult> scenarios)
    {
        text.AppendLine("## 1. Résumé des scénarios");
        text.AppendLine();
        text.AppendLine("Profit net = argent à la fin - dette - capital de départ : salaires, soins et recrues déjà payés.");
        text.AppendLine();
        text.AppendLine("| Scénario | Profit net | Par jour | Trajets | Salaires | Soins | Recrues | Butin (part des recettes) | Combats V/D | Groupe final | Fin |");
        text.AppendLine("|---|---:|---:|---:|---:|---:|---:|---:|---:|---|---|");
        foreach (ScenarioResult s in scenarios)
        {
            text.AppendLine("| " + s.Name + " | " + Money(s.NetProfit) + " | " + Money((long)s.ProfitPerDay) + " | " + s.Trips.Count + " | " + Money(s.WagesPaid)
                + " | " + Money(s.HealingCost) + " | " + Money(s.RecruitCost) + " (" + s.Recruits + ") | " + Money(s.LootTotal) + " (" + LootShareText(s) + ") | "
                + s.Victories + " / " + s.Defeats + " | " + s.EndingParty + " | " + s.EndReason + " |");
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
        text.AppendLine("| Jour | Trajet | Marchandise | Quantité | Coût | Recette | Profit | Profit prévu | Vente hors profil |");
        text.AppendLine("|---:|---|---|---:|---:|---:|---:|---:|---|");
        for (int i = 0; i < reference.Trips.Count; i++)
        {
            if (i >= 12 && i < reference.Trips.Count - 4)
            {
                if (i == 12)
                {
                    text.AppendLine("| ... | | | | | | | | |");
                }

                continue;
            }

            TripRecord t = reference.Trips[i];
            text.AppendLine("| " + t.StartDay + " | " + t.From + " > " + t.To + " | " + t.Good.DisplayName + " | " + t.Quantity + " | " + Money(t.Cost)
                + " | " + Money(t.Revenue) + " | " + Money(t.Profit) + " | " + Money(t.ExpectedProfit) + " | " + (t.ToOffProfile ? "oui" : "") + " |");
        }

        text.AppendLine();
    }

    private static void AppendOffProfile(StringBuilder text, SimulationRun run)
    {
        text.AppendLine("## 3 bis. Ventes hors profil (interdites : une ville n'achète que ce qu'elle produit ou consomme ; ce tableau doit rester à 0)");
        text.AppendLine();
        text.AppendLine("| Scénario | Trajets hors profil | Part des trajets | Profit de ces trajets | Part du profit du commerce | Dont épices |");
        text.AppendLine("|---|---:|---:|---:|---:|---:|");
        foreach (ScenarioResult s in new[] { run.Greedy, run.Reference, run.Loop, run.ReferenceLong })
        {
            List<TripRecord> off = s.Trips.Where(t => t.ToOffProfile).ToList();
            long total = s.Trips.Sum(t => (long)t.Profit);
            long offProfit = off.Sum(t => (long)t.Profit);
            int spices = off.Count(t => IsSpice(t.Good));
            text.AppendLine("| " + s.Name + " | " + off.Count + " | " + Percent(Share(off.Count, s.Trips.Count)) + " | " + Money(offProfit) + " | "
                + Percent(total > 0 ? offProfit / (double)total : 0.0) + " | " + spices + " trajet(s), " + Money(off.Where(t => IsSpice(t.Good)).Sum(t => (long)t.Profit)) + " |");
        }

        text.AppendLine();
    }

    private static void AppendPrices(StringBuilder text, ScenarioResult reference, IReadOnlyList<GoodDefinition> goods, PricingSettings pricing)
    {
        text.AppendLine("## 4. Évolution des prix (scénario B)");
        text.AppendLine();
        text.AppendLine("Prix médian (sans écart acheteur/vendeur) : moyenne, minimum et maximum sur la durée, par ville. `profil` = la ville produit ou consomme la marchandise ; "
            + "`hors profil` = elle ne l'achète pas (une ville n'achète que ce qu'elle produit ou consomme) : ce prix n'est jamais appliqué. Entre parenthèses : part des jours au prix plancher (×" + pricing.MinFactor.ToString("0.##", CultureInfo.InvariantCulture)
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
                text.AppendLine("| " + s.Town + " | " + (s.InProfile ? "profil" : "hors profil (n'achète pas)") + " | " + Money((long)s.MidMean) + " | " + Money(s.MidMin) + " | " + Money(s.MidMax)
                    + " | " + s.StockMin + "-" + s.StockMax + " | " + Percent(Share(s.DaysAtFloor, s.Samples)) + " / " + Percent(Share(s.DaysAtCeiling, s.Samples))
                    + " | " + Percent(Share(s.DaysZeroStock, s.Samples)) + " |");
            }

            text.AppendLine();
        }
    }

    // ---- Measured fights -----------------------------------------------------------------------------------------

    private static void AppendMeasuredFights(StringBuilder text, SimulationRun run)
    {
        text.AppendLine("## 4 bis. Combats mesurés (banc d'essai)");
        text.AppendLine();
        if (run.Odds == null || run.Odds.Entries.Count == 0)
        {
            text.AppendLine("Aucune table de combats mesurés : les combats suivent le modèle simple de `SimulationSettings`.");
            text.AppendLine();
            return;
        }

        text.AppendLine(run.Odds.Source);
        text.AppendLine();
        text.AppendLine("| Caravane | Bande | Victoires | PV perdus si victoire (moyenne / pire) | Unités perdues par victoire |");
        text.AppendLine("|---|---|---:|---:|---:|");
        foreach (CombatOddsTable.Entry e in run.Odds.Entries)
        {
            text.AppendLine("| " + e.caravan + " | " + e.band + " | " + e.wins + " / " + e.fights + " (" + Percent(e.WinShare) + ") | "
                + (e.wins > 0 ? Percent(e.meanHealthLost) + " / " + Percent(e.maxHealthLost) : "-") + " | " + (e.wins > 0 ? e.deathsPerWin.ToString("0.00", CultureInfo.InvariantCulture) : "-") + " |");
        }

        text.AppendLine();
    }

    // ---- The full loop -------------------------------------------------------------------------------------------

    private static void AppendLoop(StringBuilder text, SimulationSettings settings, SimulationRun run)
    {
        ScenarioResult c = run.Loop;
        text.AppendLine("## 5. La boucle complète : acheter, voyager, combattre, se soigner, recruter, payer, vendre (scénario C)");
        text.AppendLine();
        text.AppendLine("Rencontres : " + Percent(settings.EncounterChancePerDay) + " de chance par jour de voyage de croiser une des bandes présentes sur la carte. "
            + "Le marchand voit la bande et sa composition (comme le joueur) : il ne combat que si sa chance de victoire atteint " + Percent(settings.FightThreshold)
            + ", sinon il la contourne (détour de " + (settings.DetourMinutes / 60f).ToString("0.#", CultureInfo.InvariantCulture) + " h, la caravane étant plus rapide que les bandes) ; un contournement rate dans "
            + Percent(settings.AvoidFailureChance) + " des cas et le combat est alors forcé. "
            + "Chance de victoire et blessures : celles des **vrais combats mesurés** (§ 4 bis) pour la composition du groupe contre celle de la bande, réduites en proportion quand le groupe est sous "
            + Percent(settings.FitHealthShare) + " de ses PV ; une unité peut mourir selon la fréquence mesurée (un survivant au moins). "
            + "Le butin est celui de la table de la bande (U.V. et marchandises ; les marchandises qui ne rentrent pas sont perdues) ; une bande battue revient après le délai des réglages de retour, avec un nouveau butin. "
            + "En ville, le marchand garde " + settings.ReserveDays + " jours de salaires en réserve (sous " + settings.SpendAllBelowReserves.ToString("0.#", CultureInfo.InvariantCulture)
            + " fois la réserve, il dépense tout : un marchand pauvre doit commercer pour vivre), soigne à l'hôpital (au complet) toute unité sous " + settings.HospitalThresholdPercent
            + " % de ses PV si la réserve reste couverte, et recrute " + run.RecruitName + " (" + Money(run.RecruitPrice) + ") jusqu'à " + settings.TargetPartySize + " unités, sans dette et en gardant la réserve.");
        text.AppendLine();
        text.AppendLine("D'où vient et où va l'argent (graine " + c.Seed + ") :");
        text.AppendLine();
        text.AppendLine("| Poste | C | B (sans combat) |");
        text.AppendLine("|---|---:|---:|");
        ScenarioResult b = run.Reference;
        text.AppendLine("| Marge du commerce (ventes - achats, butin vendu compris) | " + Money(c.TradeMargin) + " | " + Money(b.TradeMargin) + " |");
        text.AppendLine("| Butin en U.V. | " + Money(c.LootUv) + " | " + Money(b.LootUv) + " |");
        text.AppendLine("| Salaires payés | -" + Money(c.WagesPaid) + " | -" + Money(b.WagesPaid) + " |");
        text.AppendLine("| Hôpital | -" + Money(c.HealingCost) + " | -" + Money(b.HealingCost) + " |");
        text.AppendLine("| Recrues | -" + Money(c.RecruitCost) + " | -" + Money(b.RecruitCost) + " |");
        text.AppendLine("| Dette restante | -" + Money(c.EndingDebt) + " | -" + Money(b.EndingDebt) + " |");
        text.AppendLine("| **Profit net** | **" + Money(c.NetProfit) + "** | **" + Money(b.NetProfit) + "** |");
        text.AppendLine("| Profit net par jour | " + Money((long)c.ProfitPerDay) + " | " + Money((long)b.ProfitPerDay) + " |");
        text.AppendLine("| Profit par jour avant salaires | " + Money((long)((c.NetProfit + (long)c.WagesPaid + c.EndingDebt) / (double)c.DaysElapsed)) + " | "
            + Money((long)((b.NetProfit + (long)b.WagesPaid + b.EndingDebt) / (double)b.DaysElapsed)) + " |");
        text.AppendLine("| Salaires en part de la marge du commerce | " + Percent(c.TradeMargin > 0 ? c.WagesPaid / (double)c.TradeMargin : 0.0) + " | "
            + Percent(b.TradeMargin > 0 ? b.WagesPaid / (double)b.TradeMargin : 0.0) + " |");
        text.AppendLine();
        text.AppendLine("- Rencontres : " + c.Encounters + " ; combats choisis : " + c.FightsChosen + " ; bandes contournées : " + c.Avoided + " ; combats forcés (contournement raté) : " + c.ForcedFights
            + " ; combats résolus par la mesure : " + c.MeasuredFights + ".");
        text.AppendLine("- Combats : " + c.Victories + " victoire(s), " + c.Defeats + " défaite(s) ; unités perdues : " + c.UnitsLost + " ; butin ramassé : " + Money(c.LootTotal)
            + " (perdu faute de place : " + Money(c.LootLostValue) + "), soit " + Percent(c.LootShare) + " des recettes.");
        text.AppendLine("- Soins : " + c.HitPointsRegenerated + " PV rendus par la régénération, " + c.HitPointsHealed + " PV à l'hôpital (" + c.HospitalVisits + " visite(s), " + Money(c.HealingCost) + ").");
        text.AppendLine("- Recrues : " + c.Recruits + " (" + Money(c.RecruitCost) + ") ; recrutement refusé pour dette : " + c.RecruitsRefusedForDebt + " fois ; plus grosse dette : " + Money(c.MaxDebt) + ".");
        text.AppendLine("- Marchandises restées en cargaison à la fin (au prix de base) : " + Money(c.EndingCargoValue) + " ; moyenne sur les " + run.LoopSeeds().Count + " graines : "
            + Money((long)run.LoopSeeds().Average(s => (double)s.EndingCargoValue)) + " (butin qu'aucune ville du chemin n'achetait, ou cargaison du trajet perdu).");
        text.AppendLine("- Groupe à la fin : " + c.EndingParty + ". Fin : " + c.EndReason);
        text.AppendLine();

        List<ScenarioResult> all = run.LoopSeeds();
        ScenarioResult detail = all.FirstOrDefault(s => s.Defeats == 0) ?? c;
        text.AppendLine("Trajets avec un combat (graine " + detail.Seed + (detail == c ? "" : ", la première partie menée jusqu'au bout") + ") :");
        text.AppendLine();
        text.AppendLine("| Jour | Trajet | Résultat | Valeur du butin | Profit (vente du butin comprise) |");
        text.AppendLine("|---:|---|---|---:|---:|");
        foreach (TripRecord t in detail.Trips.Where(x => x.Fight.Length > 0))
        {
            text.AppendLine("| " + t.StartDay + " | " + t.From + " > " + t.To + " (" + t.Good.DisplayName + ") | " + t.Fight + " | " + Money(t.Loot) + " | " + Money(t.Profit) + " |");
        }

        text.AppendLine();
        text.AppendLine("Effet de la chance (même scénario, " + all.Count + " graines) :");
        text.AppendLine();
        text.AppendLine("| Graine | Profit net | Par jour | Jours | V / D | Unités perdues | Butin (part) | Salaires | Hôpital | Recrues | Groupe final | Fin |");
        text.AppendLine("|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---|---|");
        foreach (ScenarioResult s in all)
        {
            text.AppendLine("| " + s.Seed + " | " + Money(s.NetProfit) + " | " + Money((long)s.ProfitPerDay) + " | " + s.DaysElapsed + " | " + s.Victories + " / " + s.Defeats + " | " + s.UnitsLost + " | "
                + Money(s.LootTotal) + " (" + Percent(s.LootShare) + ") | " + Money(s.WagesPaid) + " | " + Money(s.HealingCost) + " | " + Money(s.RecruitCost) + " | " + s.EndingParty + " | " + s.EndReason + " |");
        }

        text.AppendLine();
        text.AppendLine("Parties perdues en combat : " + all.Count(s => s.Defeats > 0) + " sur " + all.Count + ". Profit net : minimum " + Money(all.Min(s => (long)s.NetProfit))
            + ", moyenne " + Money((long)all.Average(s => (double)s.NetProfit)) + ", maximum " + Money(all.Max(s => (long)s.NetProfit))
            + ". Part du butin : de " + Percent(all.Min(s => s.LootShare)) + " à " + Percent(all.Max(s => s.LootShare)) + ".");
        List<ScenarioResult> finished = all.Where(s => s.Defeats == 0).ToList();
        if (finished.Count > 0)
        {
            text.AppendLine();
            text.AppendLine("Parties menées au bout seulement (" + finished.Count + ") : profit net moyen " + Money((long)finished.Average(s => (double)s.NetProfit))
                + " (" + Money((long)finished.Average(s => s.ProfitPerDay)) + " par jour), part du butin moyenne " + Percent(finished.Average(s => s.LootShare)) + ".");
        }

        text.AppendLine();
    }

    private static void AppendStart(StringBuilder text, SimulationRun run)
    {
        text.AppendLine("## 5 bis. Le départ de la partie manuelle");
        text.AppendLine();
        text.AppendLine("La caravane démarre avec **0 U.V.** : sans argent on ne peut rien acheter, la première somme vient du butin d'une bande (une partie en U.V., le reste en marchandises à revendre). "
            + "Butin des bandes de la carte (valeur totale tirée dans la table de la bande, U.V. et marchandises au prix de base) : "
            + (run.BandTables.Count > 0 ? string.Join(", ", run.BandTables.OrderBy(b => b.MinValue).Select(b => Money(b.MinValue) + " à " + Money(b.MaxValue))) : "aucune bande") + ". "
            + "La recrue la moins chère coûte " + Money(run.RecruitPrice) + ". Les salaires du groupe de départ courent dès la fin des jours offerts.");
        text.AppendLine();
        text.AppendLine(DescribeStart("Scénario E : un marchand prévoyant qui part avec la seule part en U.V. du plus petit butin", run.Start));
        text.AppendLine();
        text.AppendLine(DescribeStart("Scénario E2 : le même, en comptant aussi les marchandises de ce butin comme de l'argent", run.StartWithGoods));
        text.AppendLine();
        if (run.StarterSeeds.Count == 0)
        {
            return;
        }

        text.AppendLine("**Départ réel** : 0 U.V., puis le butin de la première bande que le jeu désigne (ses U.V. en caisse, ses marchandises en cargaison), avec toute la boucle (combats choisis, soins, recrues, salaires), sur "
            + run.StarterSeeds.Count + " graines :");
        text.AppendLine();
        text.AppendLine("| Graine | Butin de départ | Profit net | Capital doublé au jour | Trajets | Recrues | V / D | Fin |");
        text.AppendLine("|---:|---|---:|---:|---:|---:|---:|---|");
        foreach (ScenarioResult s in run.StarterSeeds)
        {
            int doubling = DoublingDay(s);
            text.AppendLine("| " + s.Seed + " | " + s.StartingLootText + " (" + Money(s.StartingCapital + s.StartingGoodsValue) + ") | " + Money(s.NetProfit) + " | "
                + (doubling >= 0 ? doubling.ToString(CultureInfo.InvariantCulture) : "jamais") + " | " + s.Trips.Count + " | " + s.Recruits + " | " + s.Victories + " / " + s.Defeats + " | " + s.EndReason + " |");
        }

        int stuck = run.StarterSeeds.Count(s => s.EndReason.StartsWith("Bloqué", StringComparison.Ordinal));
        text.AppendLine();
        text.AppendLine("Départs bloqués : " + stuck + " sur " + run.StarterSeeds.Count + " ; perdus en combat : " + run.StarterSeeds.Count(s => s.Defeats > 0)
            + " ; profit net moyen " + Money((long)run.StarterSeeds.Average(s => (double)s.NetProfit)) + ".");
        text.AppendLine();
    }

    // The first day the money (net of debt) reached twice the starting capital and goods; -1 when it never did.
    private static int DoublingDay(ScenarioResult run)
    {
        long start = run.StartingCapital + run.StartingGoodsValue;
        for (int d = 0; d < run.DailyBalance.Count && start > 0; d++)
        {
            if (run.DailyBalance[d] >= 2 * start)
            {
                return d;
            }
        }

        return -1;
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

        return intro + " (" + Money(run.StartingCapital) + ") finit à " + Money(run.EndingBalance) + (run.EndingDebt > 0 ? " avec une dette de " + Money(run.EndingDebt) : "")
            + " en " + run.DaysElapsed + " jours (" + run.Trips.Count + " trajets, " + Money(run.WagesPaid) + " de salaires payés, plus grosse dette " + Money(run.MaxDebt) + ")"
            + (doubling >= 0 ? " ; il double son capital au jour " + doubling + "." : " ; il ne double pas son capital sur la durée.") + " " + run.EndReason;
    }

    // ---- Effect of each mechanic ---------------------------------------------------------------------------------

    private static void AppendMechanics(StringBuilder text, SimulationRun run)
    {
        text.AppendLine("## 5 ter. Effet de chaque mécanique (une seule enlevée à la fois, même graine)");
        text.AppendLine();
        text.AppendLine("Enlever une mécanique change aussi les décisions et les tirages qui suivent (un autre trajet, un autre combat) : un écart de signe inattendu sur une seule graine n'est pas forcément une erreur.");
        text.AppendLine();
        text.AppendLine("| Comparaison | Profit net | Écart | Salaires | Hôpital | Recrues | V / D | Groupe final | Fin |");
        text.AppendLine("|---|---:|---:|---:|---:|---:|---:|---|---|");
        AppendMechanicRow(text, run.Loop, run.Loop);
        AppendMechanicRow(text, run.LoopNoHospital, run.Loop);
        AppendMechanicRow(text, run.LoopNoRecruits, run.Loop);
        AppendMechanicRow(text, run.LoopNoWages, run.Loop);
        AppendMechanicRow(text, run.Reference, run.Reference);
        AppendMechanicRow(text, run.ReferenceNoWages, run.Reference);
        text.AppendLine();
        long wagesEffect = run.ReferenceNoWages.NetProfit - (long)run.Reference.NetProfit;
        text.AppendLine("Sans combat, les salaires coûtent " + Money(wagesEffect) + " de profit sur la partie (" + Money(run.Reference.WagesPaid) + " payés ; l'écart comprend aussi la réserve qui réduit les achats).");
        text.AppendLine();
    }

    private static void AppendMechanicRow(StringBuilder text, ScenarioResult s, ScenarioResult baseline)
    {
        string delta = s == baseline ? "référence" : Money(s.NetProfit - (long)baseline.NetProfit);
        text.AppendLine("| " + s.Name + " | " + Money(s.NetProfit) + " | " + delta + " | " + Money(s.WagesPaid) + " | " + Money(s.HealingCost) + " | " + Money(s.RecruitCost)
            + " | " + s.Victories + " / " + s.Defeats + " | " + s.EndingParty + " | " + s.EndReason + " |");
    }

    // ---- Sensitivity ---------------------------------------------------------------------------------------------

    private static void AppendSensitivity(StringBuilder text, SimulationSettings settings, SimulationRun run)
    {
        text.AppendLine("## 5 quinquies. Sensibilité au contournement raté (hypothèse du simulateur)");
        text.AppendLine();
        text.AppendLine("Les chances de victoire viennent maintenant de la mesure ; l'inconnue est la part des bandes que le joueur n'arrive pas à éviter (inattention, terrain lent, bande qui coupe la route). "
            + "La boucle complète est rejouée sur les mêmes " + (settings.ExtraSeeds + 1) + " graines avec d'autres taux d'échec du contournement :");
        text.AppendLine();
        text.AppendLine("| Contournement raté | Parties menées au bout | Profit net moyen | Profit net médian | Profit par jour moyen | Part du butin moyenne | Combats gagnés par partie |");
        text.AppendLine("|---:|---:|---:|---:|---:|---:|---:|");
        AppendSensitivityRow(text, settings.AvoidFailureChance, run.LoopSeeds(), true);
        foreach (KeyValuePair<float, List<ScenarioResult>> row in run.Sensitivity)
        {
            AppendSensitivityRow(text, row.Key, row.Value, false);
        }

        text.AppendLine();
    }

    private static void AppendSensitivityRow(StringBuilder text, float chance, List<ScenarioResult> seeds, bool current)
    {
        List<long> profits = seeds.Select(s => (long)s.NetProfit).OrderBy(p => p).ToList();
        long median = profits.Count > 0 ? profits[profits.Count / 2] : 0;
        text.AppendLine("| " + Percent(chance) + (current ? " (réglage actuel)" : "") + " | " + seeds.Count(s => s.Defeats == 0) + " / " + seeds.Count + " | "
            + Money((long)seeds.Average(s => (double)s.NetProfit)) + " | " + Money(median) + " | " + Money((long)seeds.Average(s => s.ProfitPerDay)) + " | "
            + Percent(seeds.Average(s => s.LootShare)) + " | " + seeds.Average(s => (double)s.Victories).ToString("0.0", CultureInfo.InvariantCulture) + " |");
    }

    // ---- Long runs -----------------------------------------------------------------------------------------------

    private static void AppendLongRuns(StringBuilder text, SimulationSettings settings, SimulationRun run)
    {
        text.AppendLine("## 5 quater. Parties de " + settings.LongRunDays + " jours : le profit s'arrête-t-il de croître ?");
        text.AppendLine();
        foreach (ScenarioResult s in new[] { run.ReferenceLong, run.LoopLong })
        {
            List<long> slices = Slices(s, settings.GrowthWindowDays);
            text.AppendLine("- " + s.Name + " : profit net " + Money(s.NetProfit) + " en " + s.DaysElapsed + " jours (" + Money((long)s.ProfitPerDay) + " par jour). Profit par tranche de "
                + settings.GrowthWindowDays + " jours : " + (slices.Count > 0 ? string.Join(", ", slices.Select(p => Money(p))) : "partie trop courte") + ". Rapport dernier tiers / tiers du milieu : "
                + (double.IsNaN(ThirdsRatio(s)) ? "partie trop courte" : "× " + Ratio(ThirdsRatio(s))) + ". " + s.EndReason);
        }

        text.AppendLine();
    }

    // ---- Anomalies -----------------------------------------------------------------------------------------------

    private static List<string> FindAnomalies(SimulationSettings settings, PricingSettings pricing, SimulationRun run, IReadOnlyList<Market> markets)
    {
        var found = new List<string>();
        ScenarioResult reference = run.Reference;
        var every = new List<ScenarioResult>(run.Scenarios);
        every.AddRange(run.OtherSeeds);

        if (reference.Fingerprint() != run.Replay.Fingerprint())
        {
            found.Add("**Simulation non reproductible** : le scénario B rejoué avec la même graine donne une autre suite de trajets.");
        }

        if (run.Loop.Fingerprint() != run.LoopReplay.Fingerprint())
        {
            found.Add("**Simulation non reproductible** : le scénario C rejoué avec la même graine donne une autre partie (trajets, combats, salaires, soins ou recrues).");
        }

        // A profit that keeps growing (the infinite profit).
        foreach (ScenarioResult s in new[] { run.Greedy, run.Reference, run.ReferenceLong, run.LoopLong })
        {
            string growth = GrowthWithoutLimit(s, settings.GrowthWindowDays);
            if (growth.Length > 0)
            {
                found.Add("**Profit qui ne se stabilise pas** (" + s.Name + ") : " + growth);
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

        // Selling to the towns that only buy (the spices).
        long offProfit = reference.Trips.Where(t => t.ToOffProfile).Sum(t => (long)t.Profit);
        if (totalProfit > 0 && offProfit / (double)totalProfit >= OffProfileShare)
        {
            string goodsList = string.Join(", ", reference.Trips.Where(t => t.ToOffProfile).GroupBy(t => t.Good).OrderByDescending(g => g.Sum(t => (long)t.Profit))
                .Select(g => g.Key.DisplayName + " (" + g.Count() + " trajet(s), " + Money(g.Sum(t => (long)t.Profit)) + ")"));
            found.Add("**Trajets hors profil** : " + Percent(offProfit / (double)totalProfit) + " du profit du commerce (scénario B) vient de ventes à une ville qui n'a pas la marchandise dans son profil : "
                + goodsList + ". Seuil " + Percent(OffProfileShare) + ".");
        }

        foreach (ScenarioResult s in every)
        {
            foreach (string failure in s.InvariantFailures.Distinct().Take(5))
            {
                found.Add("**Incohérence** (" + s.Name + ") : " + failure);
            }
        }

        foreach (PriceSeries s in reference.Series)
        {
            if (s.Good.StablePrice)
            {
                continue;
            }

            // Outside its profile a town does not buy the good: that price is never used.
            if (s.InProfile && Share(s.DaysAtCeiling, s.Samples) >= settings.StuckShare)
            {
                found.Add("**Prix collé au plafond** : " + s.Good.DisplayName + " à " + s.Town + " (" + Percent(Share(s.DaysAtCeiling, s.Samples)) + " des jours).");
            }

            if (s.InProfile && Share(s.DaysAtFloor, s.Samples) >= settings.StuckShare)
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

        if (run.Start.EndReason.StartsWith("Bloqué", StringComparison.Ordinal) || run.Start.NetProfit <= 0)
        {
            found.Add("**Départ difficile** : avec la seule part en U.V. du plus petit butin (" + Money(run.Start.StartingCapital) + "), le marchand simulé finit à "
                + Money(run.Start.NetProfit) + " de profit net (" + run.Start.EndReason + ").");
        }

        return found;
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

    // ---- Automatic test protocol ---------------------------------------------------------------------------------

    private static List<Check> RunChecks(SimulationRun run, PayrollSettings payroll)
    {
        var checks = new List<Check>();
        var every = new List<ScenarioResult>(run.Scenarios);
        every.AddRange(run.OtherSeeds);
        every.AddRange(run.StarterSeeds);
        int failures = every.Sum(s => s.InvariantFailures.Count);
        checks.Add(new Check
        {
            Name = "T2-T3. Grand livre et invariants à chaque étape (solde = capital + ventes - achats + butin - salaires - soins - recrues ; dette ≥ 0 ; groupe et PV possibles ; cargaison ; stocks ; salaires payés + dette = salaires dus)",
            Passed = failures == 0,
            Detail = failures + " écart(s) sur " + every.Count + " parties" + (failures > 0 ? " : voir les incohérences du § 6" : "")
        });
        checks.Add(new Check
        {
            Name = "T4. Déterminisme du commerce (B et B' même graine)",
            Passed = run.Reference.Fingerprint() == run.Replay.Fingerprint(),
            Detail = run.Reference.Trips.Count + " trajets comparés"
        });
        checks.Add(new Check
        {
            Name = "T4. Déterminisme de la boucle complète (C et C' : trajets, combats, salaires, soins, recrues, groupe final)",
            Passed = run.Loop.Fingerprint() == run.LoopReplay.Fingerprint(),
            Detail = run.Loop.Trips.Count + " trajets, " + run.Loop.Victories + " victoires comparés"
        });
        List<ScenarioResult> paid = every.Where(s => s.Options.Wages && s.FirstChargedDay > 0).ToList();
        checks.Add(new Check
        {
            Name = "Jours offerts : le premier jour payé est le jour " + (payroll.GraceDays + 1) + " de la partie",
            Passed = paid.Count > 0 && paid.All(s => s.FirstChargedDay == payroll.GraceDays + 1),
            Detail = paid.Count + " parties avec salaires, premier jour payé : " + string.Join(", ", paid.Select(s => s.FirstChargedDay).Distinct())
        });
        List<ScenarioResult> noWages = every.Where(s => !s.Options.Wages).ToList();
        checks.Add(new Check
        {
            Name = "T5. Sans salaires : rien n'est payé, aucune dette",
            Passed = noWages.All(s => s.WagesPaid == 0 && s.WagesDue == 0 && s.EndingDebt == 0 && s.MaxDebt == 0),
            Detail = noWages.Count + " parties"
        });
        List<ScenarioResult> noCombat = every.Where(s => !s.Options.Combat).ToList();
        checks.Add(new Check
        {
            Name = "T5. Sans combat : aucun butin, aucun soin, aucune recrue, groupe intact",
            Passed = noCombat.All(s => s.LootTotal == 0 && s.LootUv == 0 && s.HealingCost == 0 && s.RecruitCost == 0 && s.Victories + s.Defeats == 0 && s.EndingPartySize == s.StartingPartySize),
            Detail = noCombat.Count + " parties"
        });
        checks.Add(new Check
        {
            Name = "T5. Sans hôpital : aucun soin payé",
            Passed = run.LoopNoHospital.HealingCost == 0 && run.LoopNoHospital.HospitalVisits == 0,
            Detail = "hôpital " + Money(run.LoopNoHospital.HealingCost)
        });
        checks.Add(new Check
        {
            Name = "T5. Sans recrutement : aucune recrue",
            Passed = run.LoopNoRecruits.Recruits == 0 && run.LoopNoRecruits.RecruitCost == 0,
            Detail = run.LoopNoRecruits.Recruits + " recrue(s)"
        });
        checks.Add(new Check
        {
            Name = "T5. Les salaires coûtent (B sans salaires gagne plus que B)",
            Passed = run.ReferenceNoWages.NetProfit > run.Reference.NetProfit,
            Detail = Money(run.ReferenceNoWages.NetProfit) + " contre " + Money(run.Reference.NetProfit)
        });
        checks.Add(new Check
        {
            Name = "T5. Recrutement jamais avec une dette (règle du jeu)",
            Passed = every.All(s => s.InvariantFailures.Count == 0) && every.Where(s => s.Options.Recruits).All(s => s.Recruits == 0 || s.RecruitCost == s.Recruits * run.RecruitPrice),
            Detail = every.Where(s => s.Options.Recruits).Sum(s => s.RecruitsRefusedForDebt) + " refus pour dette au total"
        });
        List<ScenarioResult> withFights = every.Where(s => s.Options.Combat).ToList();
        checks.Add(new Check
        {
            Name = "Le marchand ne combat que par choix (chance ≥ seuil) ou contournement raté ; chaque rencontre est soit un combat, soit un détour",
            Passed = withFights.All(s => s.FightsChosen + s.ForcedFights == s.Victories + s.Defeats && s.FightsChosen + s.ForcedFights + s.Avoided == s.Encounters),
            Detail = withFights.Sum(s => s.Encounters) + " rencontres : " + withFights.Sum(s => s.FightsChosen) + " combats choisis, " + withFights.Sum(s => s.ForcedFights) + " forcés, " + withFights.Sum(s => s.Avoided) + " contournées"
        });
        checks.Add(new Check
        {
            Name = "Combats résolus par la table mesurée (aucun repli sur le modèle simple)",
            Passed = run.Odds == null || withFights.All(s => s.MeasuredFights == s.Victories + s.Defeats),
            Detail = withFights.Sum(s => s.MeasuredFights) + " sur " + withFights.Sum(s => s.Victories + s.Defeats) + " combats"
        });
        checks.Add(new Check
        {
            Name = "Aucune vente à une ville hors de son profil (une ville n'achète que ce qu'elle produit ou consomme)",
            Passed = every.All(s => s.Trips.All(t => !t.ToOffProfile)),
            Detail = every.Sum(s => s.Trips.Count(t => t.ToOffProfile)) + " trajet(s) hors profil sur " + every.Sum(s => s.Trips.Count)
        });
        checks.Add(new Check
        {
            Name = "T6. Pas de dépassement (soldes bien sous la limite des entiers)",
            Passed = every.All(s => s.EndingBalance < int.MaxValue / 2 && s.Sales < long.MaxValue / 4),
            Detail = "solde le plus haut : " + Money(every.Max(s => (long)s.EndingBalance))
        });
        return checks;
    }

    private static void AppendChecks(StringBuilder text, List<Check> checks)
    {
        text.AppendLine("## 7. Protocole de test automatique (refait à chaque rapport)");
        text.AppendLine();
        text.AppendLine("| Contrôle | Résultat | Détail |");
        text.AppendLine("|---|---|---|");
        foreach (Check check in checks)
        {
            text.AppendLine("| " + check.Name + " | " + (check.Passed ? "réussi" : "**ÉCHEC**") + " | " + check.Detail + " |");
        }

        text.AppendLine();
        text.AppendLine("Les tests des règles pures (T1), de non-régression du jeu (T7) et la relecture des chiffres (T8) sont faits à part, en Play Mode : voir `PROJECT_LOG.md`.");
        text.AppendLine();
    }

    // ---- Settings to adjust --------------------------------------------------------------------------------------

    private static void AppendParameters(StringBuilder text, SimulationSettings settings, PricingSettings pricing, SimulationRun run, List<string> anomalies)
    {
        text.AppendLine("## 8. Réglages à ajuster (suggestions, rien n'a été changé)");
        text.AppendLine();
        text.AppendLine("Valeurs actuelles : écart acheteur/vendeur " + pricing.SpreadPercent.ToString("0.#", CultureInfo.InvariantCulture) + " %, facteur de prix de ×"
            + pricing.MinFactor.ToString("0.##", CultureInfo.InvariantCulture) + " à ×" + pricing.MaxFactor.ToString("0.##", CultureInfo.InvariantCulture)
            + ", capital de départ simulé " + Money(settings.StartingCapital) + ".");
        text.AppendLine();
        text.AppendLine("Suggestions tirées des mesures :");
        text.AppendLine();
        foreach (string hint in MeasuredHints(settings, run))
        {
            text.AppendLine("- " + hint);
        }

        foreach (string hint in anomalies.Select(HintFor).Where(h => h.Length > 0).Distinct())
        {
            text.AppendLine("- " + hint);
        }

        text.AppendLine();
        text.AppendLine("| Réglage | Où | Effet |");
        text.AppendLine("|---|---|---|");
        text.AppendLine("| Facteurs minimum et maximum, courbe de prix | `PricingSettings` | Bornent la marge d'une unité et le rendement d'un trajet. |");
        text.AppendLine("| Écart acheteur/vendeur (`spreadPercent`) | `PricingSettings` | Coût fixe de chaque aller-retour ; plus il est grand, moins les petites routes rapportent. |");
        text.AppendLine("| Retour vers la cible (`reversionPercent`) | `EconomySettings` | Vitesse à laquelle un marché vidé ou inondé retrouve son prix. |");
        text.AppendLine("| Production, consommation, stock cible ; stock cible hors profil (`defaultTargetStock`) | `TownDefinition` (5 villes) | Quelles routes existent, et combien une ville paie ce qu'elle n'a pas. |");
        text.AppendLine("| Masse et volume de la cargaison | `CargoCapacity_Caravan` | Plafond du profit par trajet. |");
        text.AppendLine("| Salaire par jour | `UnitDefinition.dailyWage` (`Def_*`) | Coût du voyage. |");
        text.AppendLine("| Jours offerts, dette qui bloque le recrutement | `PayrollSettings_Default` | Départ de la partie. |");
        text.AppendLine("| Régénération, prix du soin par PV | `HealingSettings_Default` | Coût des blessures. |");
        text.AppendLine("| Prix des recrues | `RecruitmentSettings` | Coût d'un groupe complet. |");
        text.AppendLine("| Butin des bandes (valeur min-max, part en U.V.) | `LootTable` des bandes | Poids des combats dans les recettes. |");
        text.AppendLine("| Délai de retour des bandes | `BandRespawnSettings_Default` | Nombre de combats possibles, donc de butin. |");
        text.AppendLine("| Hypothèses du simulateur (rencontres, victoire, dégâts, réserve, seuil d'hôpital) | `SimulationSettings` | Pas des réglages du jeu : seulement la façon de jouer du marchand simulé. |");
        text.AppendLine();
    }

    private static IEnumerable<string> MeasuredHints(SimulationSettings settings, SimulationRun run)
    {
        ScenarioResult b = run.Reference;
        ScenarioResult c = run.Loop;
        List<ScenarioResult> seeds = run.LoopSeeds();
        double wageShare = b.TradeMargin > 0 ? b.WagesPaid / (double)b.TradeMargin : 0.0;
        if (wageShare < 0.10)
        {
            yield return "**Salaires** : ils ne pèsent que " + Percent(wageShare) + " de la marge du commerce (scénario B, " + Money(b.WagesPaid) + " sur " + Money(b.TradeMargin)
                + "). Si le voyage doit vraiment coûter en plein régime, les augmenter (par exemple × 3), ou les faire croître avec la cargaison ; ils pèsent en revanche au départ (§ 5 bis).";
        }
        else if (wageShare > 0.5)
        {
            yield return "**Salaires** : ils mangent " + Percent(wageShare) + " de la marge du commerce : les baisser, ou le commerce ne vaut presque plus le voyage.";
        }

        double share = seeds.Average(s => s.LootShare);
        if (share > settings.LootShareMax)
        {
            yield return "**Butin** : " + Percent(share) + " des recettes en moyenne (au-dessus de " + Percent(settings.LootShareMax) + ") : baisser la valeur des tables de butin ou allonger le délai de retour des bandes.";
        }
        else if (share < settings.LootShareMin)
        {
            yield return "**Butin** : " + Percent(share) + " des recettes en moyenne (sous " + Percent(settings.LootShareMin) + ") : les combats rapportent peu face au commerce ; relever les tables ou raccourcir le délai de retour.";
        }

        int lost = seeds.Count(s => s.Defeats > 0);
        if (lost * 2 > seeds.Count)
        {
            double fights = seeds.Average(s => (double)(s.Victories + s.Defeats));
            yield return "**Survie** : " + lost + " parties sur " + seeds.Count + " perdues en combat, après " + fights.ToString("0.0", CultureInfo.InvariantCulture)
                + " combat(s) en moyenne, alors que le marchand ne combat qu'à " + Percent(settings.FightThreshold) + " de chances ou plus : les défaites viennent des combats forcés et des combats à 85-95 %. "
                + "Pistes côté jeu : qu'une défaite coûte (cargaison, argent, blessés) sans finir la partie ; un groupe de départ plus solide (le départ F + A perd souvent contre une bande égale) ; ou des bandes plus faciles à éviter.";
        }

        if (c.RecruitsRefusedForDebt > 0)
        {
            yield return "**Dette** : le recrutement a été refusé " + c.RecruitsRefusedForDebt + " fois pour dette (scénario C) : la dette bloque bien, à juger si c'est trop punitif.";
        }

        if (run.Start.EndReason.StartsWith("Bloqué", StringComparison.Ordinal) || run.Start.NetProfit <= 0)
        {
            yield return "**Départ** : partir de " + Money(run.Start.StartingCapital) + " ne suffit pas au marchand simulé, avec les salaires (" + Money(run.Start.WagesPaid)
                + " payés, dette maximale " + Money(run.Start.MaxDebt) + ") : un petit capital de départ, ou plus de jours offerts.";
        }
    }

    private static string HintFor(string anomaly)
    {
        if (anomaly.Contains("Rendement d'un trajet") || anomaly.Contains("Marge théorique"))
        {
            return "**Marges** : resserrer les bornes de prix (`minFactor` plus haut, `maxFactor` plus bas) ou élargir l'écart acheteur/vendeur, pour réduire la marge maximale d'un trajet.";
        }

        if (anomaly.Contains("Profit qui ne se stabilise pas"))
        {
            return "**Profit qui croît** : réduire la capacité de la cargaison ou le retour vers la cible des marchés, pour que le profit par trajet se stabilise.";
        }

        if (anomaly.Contains("hors profil") || anomaly.Contains("Profit concentré"))
        {
            return "**Épices et ventes hors profil** : les villes paient au plafond ce qu'elles n'ont pas : relever `defaultTargetStock` des profils (ou borner le prix d'une marchandise hors profil), pour qu'elles ne soient pas l'unique source de marge.";
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

    private static void AppendLimits(StringBuilder text, SimulationSettings settings)
    {
        text.AppendLine("## 9. Limites de cette simulation");
        text.AppendLine();
        text.AppendLine("- Le marchand simulé est un optimiseur à courte vue (le meilleur profit par jour du prochain trajet) : un humain peut faire mieux (enchaîner des routes) ou moins bien.");
        text.AppendLine("- Les chances de victoire et les blessures viennent de vrais combats de l'arène joués par l'IA des deux côtés (§ 4 bis) : l'IA ne fait ni pause, ni focalisation, ni repli malin, c'est une borne basse de ce que fait un joueur. "
            + "Les rencontres restent tirées au sort (" + "chance par jour de voyage) et le contournement est une hypothèse ; un groupe blessé est jugé en réduisant la chance mesurée en pleine santé.");
        text.AppendLine("- Une défaite met fin à la campagne (comme dans le jeu aujourd'hui) : les moyennes sur les graines mélangent des parties complètes et des parties courtes. "
            + "La cargaison du trajet perdu est achetée mais jamais vendue : une partie courte peut afficher une marge du commerce négative, et une part du butin sans signification (« commerce à perte »).");
        text.AppendLine("- La part du butin est une estimation : le butin est compté au prix de base, alors que ses marchandises sont vendues au prix du marché avec la cargaison (elles sont aussi dans la marge du commerce).");
        text.AppendLine("- Les règles du marchand (réserve de " + settings.ReserveDays + " jours de salaires, hôpital sous " + settings.HospitalThresholdPercent + " %, recrues jusqu'à "
            + settings.TargetPartySize + ") sont simples et fixes ; un joueur peut décider autrement.");
        text.AppendLine("- Les bandes sont rencontrées au hasard sur la route ; leurs patrouilles et leur errance réelles ne sont pas modélisées.");
        text.AppendLine("- Le temps passé en ville est nul (comme dans le jeu) ; l'hôpital et le recrutement se paient directement, sans passer par leurs fenêtres.");
        text.AppendLine("- Les durées de trajet sont celles du chemin NavMesh à vitesse de base ralentie par les terrains traversés ; un détour décidé par le joueur n'est pas simulé.");
        text.AppendLine("- Toutes les parties s'enchaînent sur le vrai calendrier (en double précision) : le rapport est identique d'une exécution à l'autre, quel que soit le jour où elle commence.");
        text.AppendLine();
    }

    // ---- Helpers -------------------------------------------------------------------------------------------------

    // Profit of each slice of 'window' days (from the net balance of each day).
    private static List<long> Slices(ScenarioResult s, int window)
    {
        var profits = new List<long>();
        for (int start = 0; start + window < s.DailyBalance.Count; start += window)
        {
            profits.Add(s.DailyBalance[start + window] - (long)s.DailyBalance[start]);
        }

        return profits;
    }

    // The profit of the last slice over the one of the second (the first holds the start). NaN when too short.
    private static double LastToSecondRatio(ScenarioResult s, int window)
    {
        List<long> slices = Slices(s, window);
        if (slices.Count < 3 || slices[1] <= 0)
        {
            return double.NaN;
        }

        return slices[slices.Count - 1] / (double)slices[1];
    }

    // The profit of the last third of the run over the one of the middle third. NaN when too short.
    private static double ThirdsRatio(ScenarioResult s)
    {
        int n = s.DailyBalance.Count;
        if (n < 30)
        {
            return double.NaN;
        }

        int third = n / 3;
        long middle = s.DailyBalance[2 * third] - (long)s.DailyBalance[third];
        long last = s.DailyBalance[n - 1] - (long)s.DailyBalance[2 * third];
        return middle > 0 ? last / (double)middle : double.NaN;
    }

    // Describes a profit that rises from slice to slice of 'window' days; empty when it levels off.
    private static string GrowthWithoutLimit(ScenarioResult s, int window)
    {
        List<long> profits = Slices(s, window);
        if (profits.Count < 3)
        {
            return string.Empty;
        }

        int n = profits.Count;
        bool growing = profits[n - 1] > profits[n - 2] * 1.1 && profits[n - 2] > profits[n - 3] * 1.1 && profits[n - 3] > 0;
        return growing ? "profit par tranche de " + window + " jours : " + string.Join(", ", profits.Select(p => Money(p))) + " (plus de 10 % de hausse à chaque tranche)." : string.Empty;
    }

    // The share of the loot, or why it means nothing (the trade lost money, so the loot is all the income).
    private static string LootShareText(ScenarioResult s)
    {
        return s.LootTotal > 0 && s.TradeMargin <= 0 ? "commerce à perte" : Percent(s.LootShare);
    }

    private static bool IsSpice(GoodDefinition good)
    {
        return good != null && (good.Id.IndexOf("spice", StringComparison.OrdinalIgnoreCase) >= 0 || good.DisplayName.IndexOf("pice", StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static string Verdict(bool passed)
    {
        return passed ? "**respecté**" : "**non respecté**";
    }

    private static string Ratio(double value)
    {
        return double.IsNaN(value) ? "-" : value.ToString("0.00", CultureInfo.InvariantCulture);
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

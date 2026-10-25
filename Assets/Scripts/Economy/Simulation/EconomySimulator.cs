using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// Plays the economy without any rendering, to check that it is believable. A merchant (the TradePlanner) buys, walks
/// the real travel times of the map, maybe fights (SimulationCombat) and sells, day after day, ONLY through the trade
/// service and the real clock, so the markets live and the rules are the player's. Its group (SimulatedParty) starts as
/// the real roster: it is paid each day with the game's payroll rule (PayrollRules: free days, debt paid first), heals
/// by the game's natural healing, goes to the hospital and recruits in town by the simple rules of the simulation
/// settings, and keeps a reserve of wages. The real payroll is switched off during the run (the simulated group is
/// paid instead) and switched back on at the end. Several scenarios are run from the same starting world (the markets,
/// wallet, cargo and calendar are put back between them), every U.V. is booked in a ledger checked at each step, and
/// the report is written to a file. Meant for Play Mode in the campaign scene: it changes the live state of that
/// session, nothing is saved.
/// </summary>
public class EconomySimulator : MonoBehaviour
{
    private const float MinutesPerDay = 1440f;
    private const float StartHour = 8f;

    [SerializeField] private SimulationSettings settings;
    [Tooltip("The same settings as the map: how long a beaten band stays away.")]
    [SerializeField] private BandRespawnSettings respawnSettings;
    [SerializeField] private TradeService tradeService;
    [SerializeField] private Wallet wallet;
    [SerializeField] private Inventory inventory;
    [SerializeField] private CampaignClock clock;
    [SerializeField] private EconomyTicker ticker;
    [SerializeField] private PricingSettings pricingSettings;
    [SerializeField] private EconomySettings economySettings;
    [SerializeField] private TerrainSettings terrainSettings;
    [Tooltip("Where the caravan stands: the merchant starts in the nearest town.")]
    [SerializeField] private Transform caravan;
    [Tooltip("Gives the starting group (the caravan's roster), the largest group and the offers of recruits.")]
    [SerializeField] private TownRecruitment recruitment;
    [Tooltip("The game's natural healing and hospital price.")]
    [SerializeField] private HealingSettings healingSettings;
    [Tooltip("The game's free days and the debt rule.")]
    [SerializeField] private PayrollSettings payrollSettings;
    [Tooltip("Optional: what real fights came to on the combat test bench. Without it (or for a matchup it lacks), the fights follow the simple model of the simulation settings.")]
    [SerializeField] private CombatOddsTable combatOdds;
    [Tooltip("Optional: the caravan's real payroll, switched off during the run (the simulated group is paid instead).")]
    [SerializeField] private PartyPayroll payroll;
    [Tooltip("Where the report is written, relative to the project folder.")]
    [SerializeField] private string reportPath = "ECONOMY_REPORT.md";

    private IPricingRules rules;
    private IReadOnlyList<Market> markets;
    private IReadOnlyList<GoodDefinition> goods;
    private TravelTimeTable travel;
    private PriceSeries[,] pendingSeries;

    // The run in progress.
    private ScenarioResult current;
    private SimulatedParty party;
    private long expectedBalance;
    private int runStartDay;
    private int debt;
    private int startingCount;
    private int recruitPrice;
    private UnitDefinition recruitDefinition;

    /// <summary>The text of the last report.</summary>
    public string LastReport { get; private set; } = string.Empty;

    /// <summary>The runs of the last report (for the tests).</summary>
    public SimulationRun LastRun { get; private set; }

    private void Start()
    {
        if (settings == null || tradeService == null || wallet == null || inventory == null || clock == null || ticker == null
            || pricingSettings == null || economySettings == null || caravan == null || recruitment == null || healingSettings == null || payrollSettings == null)
        {
            Debug.LogError("EconomySimulator: a serialized reference is not assigned.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (clock != null)
        {
            clock.DayStarted += OnDayStarted;
        }
    }

    private void OnDisable()
    {
        if (clock != null)
        {
            clock.DayStarted -= OnDayStarted;
        }
    }

    /// <summary>Runs every scenario and writes the report. Returns the report text.</summary>
    public string Run()
    {
        if (!enabled)
        {
            Debug.LogError("EconomySimulator: it is disabled (see the first error).", this);
            return string.Empty;
        }

        if (!Application.isPlaying || Market.All.Count < 2)
        {
            Debug.LogError("EconomySimulator: run it in Play Mode, in a scene with at least two markets.", this);
            return string.Empty;
        }

        markets = new List<Market>(Market.All);
        goods = markets[0].AllGoods;
        rules = new PricingRules(pricingSettings);
        travel = TravelTimeTable.Build(markets, terrainSettings, clock.MinutesPerMeter);
        FindCheapestRecruit();
        int startIndex = NearestMarket();
        int seed = settings.Seed;
        int capital = settings.StartingCapital;

        // The simulated group is paid by the simulator: the real payroll would take a second set of wages.
        bool payrollWasOn = payroll != null && payroll.enabled;
        if (payroll != null)
        {
            payroll.enabled = false;
        }

        var run = new SimulationRun { RecruitPrice = recruitPrice, RecruitName = recruitDefinition != null ? recruitDefinition.name : "-", Odds = combatOdds };
        try
        {
            run.BandTables.AddRange(CollectBandTables());
            run.StartingParty = new SimulatedParty(StartingDefinitions()).Describe();
            run.Greedy = RunScenario("A. Marchand glouton, sans combat", ScenarioOptions.Trade(SimulationStrategy.Greedy), seed, startIndex, capital);
            run.Reference = RunScenario("B. Marchand prévoyant, sans combat", ScenarioOptions.Trade(SimulationStrategy.Foresight), seed, startIndex, capital);
            run.Replay = RunScenario("B'. Scénario B rejoué (contrôle de graine)", ScenarioOptions.Trade(SimulationStrategy.Foresight), seed, startIndex, capital);
            run.Loop = RunScenario("C. Boucle complète (combats, butin, soin, recrues, salaires)", ScenarioOptions.FullLoop(), seed, startIndex, capital);
            run.LoopReplay = RunScenario("C'. Scénario C rejoué (contrôle de graine)", ScenarioOptions.FullLoop(), seed, startIndex, capital);
            for (int i = 1; i <= settings.ExtraSeeds; i++)
            {
                run.OtherSeeds.Add(RunScenario("C (graine " + (seed + i) + ")", ScenarioOptions.FullLoop(), seed + i, startIndex, capital));
            }

            // The real game starts with an empty wallet: the first money is the money part of the loot of the poorest
            // band (the least value its table can draw, times the share of the money in the split).
            int smallestLoot = run.BandTables.Count > 0 ? run.BandTables.Min(table => (int)(table.MinValue * (double)table.UvWeight / table.TotalWeight)) : 0;
            run.Start = RunScenario("E. Départ réaliste (capital = argent du plus petit butin), sans combat", ScenarioOptions.Trade(SimulationStrategy.Foresight), seed, startIndex, smallestLoot);
            // The same start, counting the goods of the poorest band as money (they are sold to the first town).
            int smallestValue = run.BandTables.Count > 0 ? run.BandTables.Min(table => table.MinValue) : 0;
            run.StartWithGoods = RunScenario("E2. Départ réaliste (capital = valeur complète du plus petit butin), sans combat", ScenarioOptions.Trade(SimulationStrategy.Foresight), seed, startIndex, smallestValue);

            // The real start of the game: 0 U.V., then the thief's loot (the first band the player is sent after), on
            // every seed, with the whole loop (fights, care, recruits, wages).
            if (settings.StarterLoot != null)
            {
                for (int i = 0; i <= settings.ExtraSeeds; i++)
                {
                    ScenarioOptions starter = ScenarioOptions.FullLoop();
                    starter.StartingLoot = settings.StarterLoot;
                    run.StarterSeeds.Add(RunScenario("Départ réel : butin du " + settings.StarterLoot.name.Replace("Loot_", string.Empty) + " (graine " + (seed + i) + ")", starter, seed + i, startIndex, 0));
                }
            }

            // One mechanic off at a time, to measure what each one does.
            ScenarioOptions noHospital = ScenarioOptions.FullLoop();
            noHospital.Healing = false;
            run.LoopNoHospital = RunScenario("C sans hôpital", noHospital, seed, startIndex, capital);
            ScenarioOptions noRecruits = ScenarioOptions.FullLoop();
            noRecruits.Recruits = false;
            run.LoopNoRecruits = RunScenario("C sans recrutement", noRecruits, seed, startIndex, capital);
            ScenarioOptions noWages = ScenarioOptions.FullLoop();
            noWages.Wages = false;
            run.LoopNoWages = RunScenario("C sans salaires", noWages, seed, startIndex, capital);
            ScenarioOptions tradeNoWages = ScenarioOptions.Trade(SimulationStrategy.Foresight);
            tradeNoWages.Wages = false;
            run.ReferenceNoWages = RunScenario("B sans salaires", tradeNoWages, seed, startIndex, capital);

            // The long runs: does the profit ever stop growing?
            ScenarioOptions longTrade = ScenarioOptions.Trade(SimulationStrategy.Foresight);
            longTrade.Days = settings.LongRunDays;
            run.ReferenceLong = RunScenario("B sur " + settings.LongRunDays + " jours", longTrade, seed, startIndex, capital);
            ScenarioOptions longLoop = ScenarioOptions.FullLoop();
            longLoop.Days = settings.LongRunDays;
            run.LoopLong = RunScenario("C sur " + settings.LongRunDays + " jours", longLoop, seed, startIndex, capital);

            // The survival rests on how often going around a band fails: the full loop on every seed with other chances.
            foreach (float chance in settings.SensitivityAvoidFailures)
            {
                var seeds = new List<ScenarioResult>();
                ScenarioOptions options = ScenarioOptions.FullLoop();
                options.AvoidFailure = chance;
                for (int i = 0; i <= settings.ExtraSeeds; i++)
                {
                    seeds.Add(RunScenario("C, contournement raté " + Mathf.RoundToInt(chance * 100f) + " % (graine " + (seed + i) + ")", options, seed + i, startIndex, capital));
                }

                run.Sensitivity.Add(new KeyValuePair<float, List<ScenarioResult>>(chance, seeds));
            }

            run.Scenarios.AddRange(new[] { run.Greedy, run.Reference, run.Replay, run.Loop, run.LoopReplay, run.Start, run.StartWithGoods,
                run.LoopNoHospital, run.LoopNoRecruits, run.LoopNoWages, run.ReferenceNoWages, run.ReferenceLong, run.LoopLong });
            ResetWorld();
        }
        finally
        {
            current = null;
            if (payroll != null)
            {
                payroll.ClearDebt();
                payroll.enabled = payrollWasOn;
            }
        }

        LastRun = run;
        LastReport = SimulationReportWriter.Build(settings, pricingSettings, healingSettings, payrollSettings, markets, goods, travel, run);
        WriteReport(LastReport);
        return LastReport;
    }

    private ScenarioResult RunScenario(string name, ScenarioOptions options, int seed, int startIndex, int capital)
    {
        current = null;
        ResetWorld();
        var result = new ScenarioResult
        {
            Name = name,
            Options = options,
            Strategy = options.Strategy,
            Combat = options.Combat,
            Seed = seed,
            StartingCapital = capital
        };

        party = new SimulatedParty(StartingDefinitions());
        startingCount = party.Count;
        result.StartingPartySize = startingCount;
        debt = 0;
        expectedBalance = 0;
        Deposit(capital);
        if (options.StartingLoot != null)
        {
            StartWithLoot(options.StartingLoot, seed, result);
        }

        runStartDay = clock.Day;
        current = result;

        int days = options.Days > 0 ? options.Days : settings.Days;
        var dice = new System.Random(seed);
        var fights = new SimulationCombat(settings, respawnSettings, dice, CollectBands());
        var planner = new TradePlanner(tradeService, rules, economySettings.ReversionPercent, travel, markets, goods);
        PriceSeries[,] series = CreateSeries(result);
        int fullHealth = Math.Min(settings.TargetPartySize, recruitment.MaxSize) * (recruitDefinition != null ? recruitDefinition.MaxHealth : 1);

        double startMinutes = clock.TotalMinutes;
        ticker.DayApplied += OnDayApplied;
        pendingSeries = series;
        Sample(series);
        result.DailyBalance.Add(wallet.Balance);

        int here = startIndex;
        int idleStreak = 0;
        while (clock.Day - runStartDay < days)
        {
            // What is still in the cargo (goods of a loot this town buys) is sold first: it is money to trade with.
            SellEverything(markets[here], null, result);
            if (options.Combat)
            {
                VisitHospital(options, result);
                Recruit(options, result);
            }

            TradePlan plan = planner.Best(here, options.Strategy, clock.TotalMinutes, result, Budget(options));
            if (!plan.IsValid)
            {
                idleStreak++;
                result.IdleDays++;
                if (idleStreak > settings.MaxIdleDays)
                {
                    result.EndReason = "Bloqué : aucun trajet rentable pendant " + settings.MaxIdleDays + " jours (ville " + markets[here].TownName + ").";
                    break;
                }

                clock.AdvanceMinutes(MinutesPerDay);
                CheckInvariants(result, options);
                continue;
            }

            idleStreak = 0;
            TradeResult bought = tradeService.Buy(plan.From, plan.Good, plan.Quantity);
            if (!bought.Success)
            {
                result.RefusedTrades++;
                result.InvariantFailures.Add("Jour " + RunDay() + " : achat prévu refusé (" + plan.Good.Id + " x" + plan.Quantity
                    + " à " + plan.From.TownName + ") : " + bought.Status + ".");
                result.IdleDays++;
                clock.AdvanceMinutes(MinutesPerDay);
                continue;
            }

            expectedBalance -= bought.Amount;
            result.Purchases += bought.Amount;
            if (bought.Amount != plan.Cost)
            {
                result.InvariantFailures.Add("Jour " + RunDay() + " : coût réel " + bought.Amount + " différent du coût prévu " + plan.Cost + ".");
            }

            var trip = new TripRecord
            {
                StartDay = clock.Day - runStartDay,
                From = plan.From.TownName,
                To = plan.To.TownName,
                Good = plan.Good,
                Quantity = bought.Quantity,
                Cost = bought.Amount,
                ExpectedProfit = plan.Profit,
                Minutes = plan.Minutes,
                ToOffProfile = !plan.To.IsInProfile(plan.Good)
            };
            result.Trips.Add(trip);

            // A band met on the way: the merchant sees it (and its composition) and fights only when its chance is good
            // enough; otherwise it goes around it, a detour that costs time and may fail, which forces the fight.
            FightOutcome fight = default;
            CombatOddsTable.Entry matchup = null;
            bool detour = false;
            if (options.Combat)
            {
                float travelDays = plan.Minutes / MinutesPerDay;
                SimulatedBand band = fights.Meet(travelDays, RunDay());
                if (band != null)
                {
                    result.Encounters++;
                    double chance = WinChance(band, fullHealth, out matchup);
                    bool fightIt = chance >= settings.FightThreshold;
                    if (fightIt)
                    {
                        result.FightsChosen++;
                    }
                    else if (dice.NextDouble() < AvoidFailure(options))
                    {
                        result.ForcedFights++;
                        fightIt = true;
                    }
                    else
                    {
                        result.Avoided++;
                        detour = true;
                        trip.Detour = true;
                    }

                    if (fightIt)
                    {
                        result.MeasuredFights += matchup != null ? 1 : 0;
                        trip.Band = band.Name;
                        fight = fights.Fight(band, chance, travelDays, RunDay());
                    }
                }
            }

            clock.AdvanceMinutes(plan.Minutes + (detour ? settings.DetourMinutes : 0f));
            here = plan.ToIndex;
            if (fight.Happened && !fight.Won)
            {
                trip.Fight = "Défaite";
                result.Defeats++;
                result.EndReason = "Défaite en combat sur la route " + trip.From + " > " + trip.To + " (fin de la campagne).";
                break;
            }

            if (fight.Happened)
            {
                int dead = matchup != null
                    ? party.TakeMeasuredDamage(dice, matchup.meanHealthLost, matchup.deathsPerWin)
                    : party.TakeVictoryDamage(dice, settings.VictoryDamageMinPercent, settings.VictoryDamageMaxPercent);
                trip.Fight = "Victoire" + (dead > 0 ? ", " + dead + " unité(s) perdue(s)" : "");
                result.Victories++;
                result.UnitsLost += dead;
                CollectLoot(fight.Loot, trip, result);
            }

            SellEverything(markets[here], trip, result);
            CheckInvariants(result, options);
        }

        ticker.DayApplied -= OnDayApplied;
        pendingSeries = null;
        result.EndingBalance = wallet.Balance;
        result.EndingDebt = debt;
        result.EndingParty = party.Describe();
        result.EndingPartySize = party.Count;
        for (int g = 0; g < goods.Count; g++)
        {
            result.EndingCargoValue += (long)goods[g].BasePrice * inventory.GetQuantity(goods[g]);
        }

        result.DaysElapsed = Math.Max(1, (int)Math.Ceiling((clock.TotalMinutes - startMinutes) / MinutesPerDay));
        if (string.IsNullOrEmpty(result.EndReason))
        {
            result.EndReason = "Terminé : " + days + " jours écoulés.";
        }

        if (result.WagesPaid + (long)result.EndingDebt != result.WagesDue)
        {
            result.InvariantFailures.Add("Salaires payés (" + result.WagesPaid + ") + dette (" + result.EndingDebt + ") différents des salaires dus (" + result.WagesDue + ").");
        }

        current = null;
        return result;
    }

    // Each world day: the group heals a little (the game's rule) and, after the free days, the wages are paid (the
    // game's rule: the debt first, then the day, as far as the money goes).
    private void OnDayStarted(int day)
    {
        if (current == null || party == null)
        {
            return;
        }

        current.HitPointsRegenerated += party.RegenerateDay(healingSettings);
        if (!current.Options.Wages || !PayrollRules.IsCharged(day - runStartDay + 1, payrollSettings.GraceDays))
        {
            return;
        }

        int cost = party.DailyWage;
        current.WagesDue += cost;
        if (current.FirstChargedDay == 0)
        {
            current.FirstChargedDay = day - runStartDay + 1;
        }

        PayrollRules.Settle(debt, cost, wallet.Balance, out int paid, out int debtAfter);
        if (paid > 0 && !Withdraw(paid))
        {
            current.InvariantFailures.Add("Jour " + RunDay() + " : paiement des salaires (" + paid + ") refusé par le portefeuille.");
            debtAfter += paid;
            paid = 0;
        }

        current.WagesPaid += paid;
        debt = debtAfter;
        current.MaxDebt = Math.Max(current.MaxDebt, debt);
    }

    // In town: the units under the threshold are healed to full at the hospital, at the game's price, when the money
    // left still covers the reserve.
    private void VisitHospital(ScenarioOptions options, ScenarioResult result)
    {
        if (!options.Healing)
        {
            return;
        }

        long price = party.HospitalPrice(settings.HospitalThresholdPercent, healingSettings.PricePerHitPoint);
        if (price <= 0 || price > int.MaxValue || wallet.Balance - price < Reserve(options, party.DailyWage))
        {
            return;
        }

        if (Withdraw((int)price))
        {
            result.HealingCost += (int)price;
            result.HospitalVisits++;
            result.HitPointsHealed += party.Heal(settings.HospitalThresholdPercent);
        }
    }

    // In town: the cheapest recruit joins while the group is under its target size, without debt (the game's rule)
    // and keeping the reserve of the larger group plus some money to trade with.
    private void Recruit(ScenarioOptions options, ScenarioResult result)
    {
        if (!options.Recruits || recruitDefinition == null || recruitPrice <= 0)
        {
            return;
        }

        int target = Math.Min(settings.TargetPartySize, recruitment.MaxSize);
        while (party.Count < target)
        {
            if (debt > 0 && payrollSettings.DebtBlocksRecruitment)
            {
                result.RecruitsRefusedForDebt++;
                return;
            }

            if (wallet.Balance - recruitPrice < Reserve(options, party.DailyWage + recruitDefinition.DailyWage) + settings.TradingCapitalKeptOnRecruit || !Withdraw(recruitPrice))
            {
                return;
            }

            party.Add(recruitDefinition);
            result.RecruitCost += recruitPrice;
            result.Recruits++;
        }
    }

    // What the merchant may spend on goods: its money minus the reserve; but a poor merchant (less than a few reserves)
    // spends all of it, or it could never start.
    private long Budget(ScenarioOptions options)
    {
        int balance = wallet.Balance;
        int reserve = Reserve(options, party.DailyWage);
        return balance < reserve * (double)settings.SpendAllBelowReserves ? balance : balance - (long)reserve;
    }

    // The chance to win against this band: the measured matchup of the group's composition against the band's,
    // lowered when the group is under its fit share of hit points; without a measure, the simple model.
    private double WinChance(SimulatedBand band, int fullHealth, out CombatOddsTable.Entry matchup)
    {
        matchup = combatOdds != null ? combatOdds.Find(party.Key, party.Count, band.Key) : null;
        if (matchup == null)
        {
            return settings.BaseWinChance * party.Strength(fullHealth);
        }

        return matchup.WinShare * Math.Min(1.0, party.HealthShare / settings.FitHealthShare);
    }

    private float AvoidFailure(ScenarioOptions options)
    {
        return options.AvoidFailure >= 0f ? options.AvoidFailure : settings.AvoidFailureChance;
    }

    // What the merchant keeps in the wallet: some days of the group's wages (nothing when there are no wages).
    private int Reserve(ScenarioOptions options, int dailyWage)
    {
        return options.Wages ? settings.ReserveDays * dailyWage : 0;
    }

    private int RunDay()
    {
        return clock.Day - runStartDay;
    }

    private void SellEverything(Market town, TripRecord trip, ScenarioResult result)
    {
        for (int g = 0; g < goods.Count; g++)
        {
            // A town buys only the goods of its profile: the others stay in the cargo (goods found in loot).
            if (!town.IsInProfile(goods[g]))
            {
                continue;
            }

            for (int attempt = 0; attempt < 5 && inventory.GetQuantity(goods[g]) > 0; attempt++)
            {
                TradeResult sold = tradeService.SellMax(town, goods[g]);
                if (!sold.Success)
                {
                    result.RefusedTrades++;
                    result.InvariantFailures.Add("Vente refusée : " + goods[g].Id + " à " + town.TownName + " : " + sold.Status + ".");
                    break;
                }

                expectedBalance += sold.Amount;
                result.Sales += sold.Amount;
                if (trip != null)
                {
                    trip.Revenue += sold.Amount;
                }
                else
                {
                    result.CargoSoldInTown += sold.Amount;
                }
            }
        }
    }

    // The checks of every step: the wallet against the running balance and against the ledger, the debt, the group,
    // the cargo and the stocks. A failure is noted (and shown in the report), never silent.
    private void CheckInvariants(ScenarioResult result, ScenarioOptions options)
    {
        if (wallet.Balance != expectedBalance)
        {
            result.InvariantFailures.Add("Solde " + wallet.Balance + " différent du solde attendu " + expectedBalance + ".");
            expectedBalance = wallet.Balance;
        }

        if (wallet.Balance != result.LedgerBalance)
        {
            result.InvariantFailures.Add("Jour " + RunDay() + " : solde " + wallet.Balance + " différent du grand livre " + result.LedgerBalance
                + " (capital + ventes - achats + butin - salaires - soins - recrues).");
        }

        if (debt < 0 || (!options.Wages && debt != 0))
        {
            result.InvariantFailures.Add("Dette impossible : " + debt + ".");
        }

        // Never empty (a won fight leaves one unit standing), never above the target (or the start, without recruits).
        int largest = Math.Max(startingCount, options.Recruits ? Math.Min(settings.TargetPartySize, recruitment.MaxSize) : 0);
        if (party.Count < 1 || party.Count > largest)
        {
            result.InvariantFailures.Add("Taille du groupe impossible : " + party.Count + ".");
        }

        for (int i = 0; i < party.Count; i++)
        {
            if (party.HealthOf(i) <= 0 || party.HealthOf(i) > party.MaxHealthOf(i))
            {
                result.InvariantFailures.Add("PV impossibles : " + party.Describe() + ".");
                break;
            }
        }

        CargoCapacity capacity = inventory.Capacity;
        if (inventory.TotalMass > capacity.MaxMass + 0.01f || inventory.TotalVolume > capacity.MaxVolume + 0.01f)
        {
            result.InvariantFailures.Add("Cargaison au-dessus de la capacité (" + inventory.TotalMass + " kg, " + inventory.TotalVolume + " L).");
        }

        for (int m = 0; m < markets.Count; m++)
        {
            for (int g = 0; g < goods.Count; g++)
            {
                if (markets[m].GetStock(goods[g]) < 0)
                {
                    result.InvariantFailures.Add("Stock négatif : " + goods[g].Id + " à " + markets[m].TownName + ".");
                }
            }
        }
    }

    private void OnDayApplied(int day)
    {
        if (pendingSeries != null && current != null)
        {
            Sample(pendingSeries);
            current.DailyBalance.Add(wallet.Balance - debt);
        }
    }

    private PriceSeries[,] CreateSeries(ScenarioResult result)
    {
        var series = new PriceSeries[markets.Count, goods.Count];
        for (int m = 0; m < markets.Count; m++)
        {
            for (int g = 0; g < goods.Count; g++)
            {
                var entry = new PriceSeries { Town = markets[m].TownName, Good = goods[g], InProfile = markets[m].IsInProfile(goods[g]) };
                series[m, g] = entry;
                result.Series.Add(entry);
            }
        }

        return series;
    }

    private void Sample(PriceSeries[,] series)
    {
        for (int m = 0; m < markets.Count; m++)
        {
            for (int g = 0; g < goods.Count; g++)
            {
                GoodDefinition good = goods[g];
                int stock = markets[m].GetStock(good);
                int target = markets[m].GetTarget(good);
                int mid = rules.MidPrice(good, stock, target);
                float factor = rules.PriceFactor(good, stock, target);
                PriceSeries entry = series[m, g];
                entry.Samples++;
                entry.MidSum += mid;
                entry.MidMin = Math.Min(entry.MidMin, mid);
                entry.MidMax = Math.Max(entry.MidMax, mid);
                entry.StockMin = Math.Min(entry.StockMin, stock);
                entry.StockMax = Math.Max(entry.StockMax, stock);
                if (stock == 0)
                {
                    entry.DaysZeroStock++;
                }

                if (!good.StablePrice)
                {
                    if (factor <= pricingSettings.MinFactor + 1e-4f)
                    {
                        entry.DaysAtFloor++;
                    }
                    else if (factor >= pricingSettings.MaxFactor - 1e-4f)
                    {
                        entry.DaysAtCeiling++;
                    }
                }
            }
        }
    }

    // Puts the world back to the start of a scenario: the next 08:00 of the calendar, every market at its initial
    // stock, an empty cargo and an empty wallet. The calendar is moved first (it makes the markets live a day).
    private void ResetWorld()
    {
        double into = clock.TotalMinutes % MinutesPerDay;
        double toStart = StartHour * 60.0 - into;
        clock.AdvanceMinutes(toStart > 1f ? toStart : toStart + MinutesPerDay);

        if (wallet.Balance > 0)
        {
            wallet.TryWithdraw(wallet.Balance);
        }

        for (int g = 0; g < goods.Count; g++)
        {
            int owned = inventory.GetQuantity(goods[g]);
            if (owned > 0)
            {
                inventory.TryRemove(goods[g], owned);
            }
        }

        for (int m = 0; m < markets.Count; m++)
        {
            for (int g = 0; g < goods.Count; g++)
            {
                int wanted = markets[m].Definition.TryGetEntry(goods[g], out GoodStockProfile entry) ? entry.initialStock : 0;
                int stock = markets[m].GetStock(goods[g]);
                if (wanted > stock)
                {
                    markets[m].TryAddStock(goods[g], wanted - stock);
                }
                else if (wanted < stock)
                {
                    markets[m].TryRemoveStock(goods[g], stock - wanted);
                }
            }
        }
    }

    private void Deposit(int amount)
    {
        if (amount > 0 && wallet.TryDeposit(amount) == WalletResult.Accepted)
        {
            expectedBalance += amount;
        }
    }

    private bool Withdraw(int amount)
    {
        if (amount <= 0 || wallet.TryWithdraw(amount) != WalletResult.Accepted)
        {
            return false;
        }

        expectedBalance -= amount;
        return true;
    }

    private int NearestMarket()
    {
        int best = 0;
        float bestDistance = float.MaxValue;
        for (int m = 0; m < markets.Count; m++)
        {
            float distance = (markets[m].Town.Entrance - caravan.position).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = m;
            }
        }

        return best;
    }

    // The money goes to the wallet; the goods go to the cargo as far as they fit (dearest first, as in the game), the
    // rest is lost. The value gathered and the value lost are noted.
    private void CollectLoot(LootResult loot, TripRecord trip, ScenarioResult result)
    {
        var taken = new List<GoodStack>();
        var lost = new List<GoodStack>();
        inventory.PlanAdd(loot.Goods, taken, lost);
        long gathered = loot.Uv;
        if (loot.Uv > 0)
        {
            Deposit(loot.Uv);
            result.LootUv += loot.Uv;
        }

        for (int i = 0; i < taken.Count; i++)
        {
            if (inventory.TryAdd(taken[i].Good, taken[i].Quantity) == InventoryResult.Accepted)
            {
                gathered += taken[i].BaseValue;
            }
            else
            {
                result.InvariantFailures.Add("Butin : ajout de " + taken[i].Good.Id + " refusé alors qu'il devait rentrer.");
            }
        }

        for (int i = 0; i < lost.Count; i++)
        {
            result.LootLostValue += (int)lost[i].BaseValue;
        }

        trip.Loot = (int)gathered;
        result.LootTotal += (int)gathered;
    }

    // The real start: the loot of the first band beaten (rolled with its own dice from the seed): its U.V. are the
    // capital, its goods go to the cargo (what does not fit is lost, as after a fight).
    private void StartWithLoot(LootTable table, int seed, ScenarioResult result)
    {
        LootResult loot = LootRoller.Roll(table, new System.Random(seed * 7919 + 17));
        if (loot.Uv > 0)
        {
            Deposit(loot.Uv);
            result.StartingCapital += loot.Uv;
        }

        var taken = new List<GoodStack>();
        var lost = new List<GoodStack>();
        inventory.PlanAdd(loot.Goods, taken, lost);
        var text = new System.Text.StringBuilder().Append(loot.Uv).Append(" U.V.");
        for (int i = 0; i < taken.Count; i++)
        {
            if (inventory.TryAdd(taken[i].Good, taken[i].Quantity) == InventoryResult.Accepted)
            {
                result.StartingGoodsValue += taken[i].BaseValue;
                text.Append(" + ").Append(taken[i].Quantity).Append(' ').Append(taken[i].Good.DisplayName);
            }
        }

        result.StartingLootText = text.ToString();
    }

    // The definitions of the caravan's roster: the group the merchant starts with.
    private List<UnitDefinition> StartingDefinitions()
    {
        var definitions = new List<UnitDefinition>();
        IReadOnlyList<PartyMember> members = recruitment.Party.Members;
        for (int i = 0; i < members.Count; i++)
        {
            if (members[i].definition != null)
            {
                definitions.Add(members[i].definition);
            }
        }

        return definitions;
    }

    // Every enemy band of the map: its name, its composition and its loot table.
    private static List<SimulatedBand> CollectBands()
    {
        var found = new List<SimulatedBand>();
        IReadOnlyList<UnitTeam> units = UnitTeam.All;
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i].Team != Team.Enemy || !units[i].TryGetComponent(out PartyRoster roster) || roster.LootTable == null)
            {
                continue;
            }

            var definitions = new List<UnitDefinition>();
            for (int m = 0; m < roster.Members.Count; m++)
            {
                definitions.Add(roster.Members[m].definition);
            }

            found.Add(new SimulatedBand { Name = roster.LootTable.name.Replace("Loot_", string.Empty), Key = CombatOddsTable.KeyOf(definitions), Table = roster.LootTable });
        }

        return found;
    }

    // The loot table of every enemy band of the map.
    private static List<LootTable> CollectBandTables()
    {
        var loots = new List<LootTable>();
        IReadOnlyList<UnitTeam> units = UnitTeam.All;
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i].Team == Team.Enemy && units[i].TryGetComponent(out PartyRoster roster))
            {
                if (roster.LootTable != null)
                {
                    loots.Add(roster.LootTable);
                }
            }
        }

        return loots;
    }

    // The cheapest offer of recruits: what the merchant buys, and the reference unit of a full group.
    private void FindCheapestRecruit()
    {
        recruitPrice = 0;
        recruitDefinition = null;
        IReadOnlyList<RecruitOffer> offers = recruitment.Settings.Offers;
        int best = int.MaxValue;
        for (int i = 0; i < offers.Count; i++)
        {
            if (offers[i] != null && offers[i].definition != null && offers[i].price < best)
            {
                best = offers[i].price;
                recruitPrice = offers[i].price;
                recruitDefinition = offers[i].definition;
            }
        }

        if (recruitDefinition == null)
        {
            Debug.LogError("EconomySimulator: no recruit offer with a unit: the runs with fights will not recruit.", this);
        }
    }

    private void WriteReport(string text)
    {
        if (string.IsNullOrWhiteSpace(reportPath))
        {
            return;
        }

        string full = Path.GetFullPath(Path.Combine(Application.dataPath, "..", reportPath));
        File.WriteAllText(full, text);
        Debug.Log("EconomySimulator: report written to " + full);
    }
}

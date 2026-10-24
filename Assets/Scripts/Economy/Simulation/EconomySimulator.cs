using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// Plays the economy without any rendering, to check that it is believable. A merchant (the TradePlanner) buys, walks
/// the real travel times of the map, maybe fights (SimulationCombat) and sells, day after day, ONLY through the trade
/// service and the real clock, so the markets live and the rules are the player's. Several scenarios are run from the
/// same starting world (the markets, wallet, cargo and calendar are put back between them) and the report is written
/// to a file. Meant for Play Mode in the campaign scene: it changes the live state of that session, nothing is saved.
/// </summary>
public class EconomySimulator : MonoBehaviour
{
    private const float MinutesPerDay = 1440f;
    private const float StartHour = 8f;

    [SerializeField] private SimulationSettings settings;
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
    [Tooltip("Gives the size of the party and the price of a replacement unit.")]
    [SerializeField] private TownRecruitment recruitment;
    [Tooltip("Where the report is written, relative to the project folder.")]
    [SerializeField] private string reportPath = "ECONOMY_REPORT.md";

    private IPricingRules rules;
    private IReadOnlyList<Market> markets;
    private IReadOnlyList<GoodDefinition> goods;
    private TravelTimeTable travel;
    private ScenarioResult current;
    private long expectedBalance;
    private PriceSeries[,] pendingSeries;

    /// <summary>The text of the last report.</summary>
    public string LastReport { get; private set; } = string.Empty;

    private void Start()
    {
        if (settings == null || tradeService == null || wallet == null || inventory == null || clock == null || ticker == null
            || pricingSettings == null || economySettings == null || caravan == null || recruitment == null)
        {
            Debug.LogError("EconomySimulator: a serialized reference is not assigned.", this);
            enabled = false;
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
        int startIndex = NearestMarket();

        var scenarios = new List<ScenarioResult>
        {
            RunScenario("A. Marchand glouton, sans combat", SimulationStrategy.Greedy, false, settings.Seed, startIndex, settings.StartingCapital),
            RunScenario("B. Marchand prévoyant, sans combat", SimulationStrategy.Foresight, false, settings.Seed, startIndex, settings.StartingCapital),
            RunScenario("B'. Même scénario B rejoué (contrôle de graine)", SimulationStrategy.Foresight, false, settings.Seed, startIndex, settings.StartingCapital),
            RunScenario("C. Marchand prévoyant, avec combats", SimulationStrategy.Foresight, true, settings.Seed, startIndex, settings.StartingCapital)
        };

        var otherSeeds = new List<ScenarioResult>();
        for (int i = 1; i <= settings.ExtraSeeds; i++)
        {
            otherSeeds.Add(RunScenario("C (graine " + (settings.Seed + i) + ")", SimulationStrategy.Foresight, true, settings.Seed + i, startIndex, settings.StartingCapital));
        }

        // The real game starts with an empty wallet: the first money is the money part of the loot of the poorest band
        // (the least value its table can draw, times the share of the money in the split).
        List<LootTable> tables = CollectBandTables();
        int smallestLoot = tables.Count > 0 ? tables.Min(table => (int)(table.MinValue * (double)table.UvWeight / table.TotalWeight)) : 0;
        scenarios.Add(RunScenario("E. Départ réaliste (capital = argent du plus petit butin), sans combat", SimulationStrategy.Foresight, false, settings.Seed, startIndex, smallestLoot));
        // The same start, counting the goods of the poorest band as money (they are sold to the first town).
        int smallestValue = tables.Count > 0 ? tables.Min(table => table.MinValue) : 0;
        scenarios.Add(RunScenario("E2. Départ réaliste (capital = valeur complète du plus petit butin), sans combat", SimulationStrategy.Foresight, false, settings.Seed, startIndex, smallestValue));

        ResetWorld();
        LastReport = SimulationReportWriter.Build(settings, pricingSettings, markets, goods, travel, scenarios, otherSeeds, tables, ReplacementPrice());
        WriteReport(LastReport);
        return LastReport;
    }

    private ScenarioResult RunScenario(string name, SimulationStrategy strategy, bool combat, int seed, int startIndex, int capital)
    {
        ResetWorld();
        var result = new ScenarioResult
        {
            Name = name,
            Strategy = strategy,
            Combat = combat,
            Seed = seed,
            StartingCapital = capital
        };
        current = result;
        expectedBalance = 0;
        Deposit(capital);

        var dice = new System.Random(seed);
        SimulationCombat fights = new SimulationCombat(settings, dice, CollectBandTables(), recruitment.MaxSize);
        var planner = new TradePlanner(tradeService, rules, economySettings.ReversionPercent, travel, markets, goods);
        PriceSeries[,] series = CreateSeries(result);

        float startMinutes = clock.TotalMinutes;
        int startDay = clock.Day;
        ticker.DayApplied += OnDayApplied;
        pendingSeries = series;
        Sample(series);
        result.DailyBalance.Add(wallet.Balance);

        int here = startIndex;
        int idleStreak = 0;
        int replacementPrice = ReplacementPrice();
        while (clock.Day - startDay < settings.Days)
        {
            while (combat && fights.Missing > 0 && replacementPrice > 0 && wallet.CanAfford(replacementPrice))
            {
                Withdraw(replacementPrice);
                fights.Replace();
                result.ReplacementCost += replacementPrice;
            }

            TradePlan plan = planner.Best(here, strategy, clock.TotalMinutes, result);
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
                continue;
            }

            idleStreak = 0;
            TradeResult bought = tradeService.Buy(plan.From, plan.Good, plan.Quantity);
            if (!bought.Success)
            {
                result.RefusedTrades++;
                result.InvariantFailures.Add("Jour " + (clock.Day - startDay) + " : achat prévu refusé (" + plan.Good.Id + " x" + plan.Quantity
                    + " à " + plan.From.TownName + ") : " + bought.Status + ".");
                result.IdleDays++;
                clock.AdvanceMinutes(MinutesPerDay);
                continue;
            }

            expectedBalance -= bought.Amount;
            if (bought.Amount != plan.Cost)
            {
                result.InvariantFailures.Add("Jour " + (clock.Day - startDay) + " : coût réel " + bought.Amount + " différent du coût prévu " + plan.Cost + ".");
            }

            var trip = new TripRecord
            {
                StartDay = clock.Day - startDay,
                From = plan.From.TownName,
                To = plan.To.TownName,
                Good = plan.Good,
                Quantity = bought.Quantity,
                Cost = bought.Amount,
                ExpectedProfit = plan.Profit,
                Minutes = plan.Minutes
            };
            result.Trips.Add(trip);

            FightOutcome fight = combat ? fights.Roll(plan.Minutes / MinutesPerDay) : default;
            clock.AdvanceMinutes(plan.Minutes);
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
                trip.Fight = "Victoire" + (fight.UnitsLost > 0 ? ", " + fight.UnitsLost + " unité(s) perdue(s)" : "");
                result.Victories++;
                result.UnitsLost += fight.UnitsLost;
                CollectLoot(fight.Loot, trip, result);
            }

            SellEverything(markets[here], trip, result);
            CheckInvariants(result);
        }

        ticker.DayApplied -= OnDayApplied;
        pendingSeries = null;
        result.EndingBalance = wallet.Balance;
        result.DaysElapsed = Mathf.Max(1, Mathf.CeilToInt((clock.TotalMinutes - startMinutes) / MinutesPerDay));
        if (string.IsNullOrEmpty(result.EndReason))
        {
            result.EndReason = "Terminé : " + settings.Days + " jours écoulés.";
        }

        current = null;
        return result;
    }

    private void SellEverything(Market town, TripRecord trip, ScenarioResult result)
    {
        for (int g = 0; g < goods.Count; g++)
        {
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
                trip.Revenue += sold.Amount;
            }
        }
    }

    private void CheckInvariants(ScenarioResult result)
    {
        if (wallet.Balance != expectedBalance)
        {
            result.InvariantFailures.Add("Solde " + wallet.Balance + " différent du solde attendu " + expectedBalance + ".");
            expectedBalance = wallet.Balance;
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
            current.DailyBalance.Add(wallet.Balance);
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
        float into = clock.TotalMinutes % MinutesPerDay;
        float toStart = StartHour * 60f - into;
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

    private void Withdraw(int amount)
    {
        if (wallet.TryWithdraw(amount) == WalletResult.Accepted)
        {
            expectedBalance -= amount;
        }
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
        Deposit(loot.Uv);
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

    private int ReplacementPrice()
    {
        IReadOnlyList<RecruitOffer> offers = recruitment.Settings.Offers;
        int price = int.MaxValue;
        for (int i = 0; i < offers.Count; i++)
        {
            if (offers[i] != null)
            {
                price = Math.Min(price, offers[i].price);
            }
        }

        return price == int.MaxValue ? 0 : price;
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

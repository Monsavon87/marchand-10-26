using System;
using System.Collections.Generic;

/// <summary>One trip of the simulated merchant: bought here, walked, (maybe fought), sold there.</summary>
public class TripRecord
{
    public int StartDay;
    public string From;
    public string To;
    public GoodDefinition Good;
    public int Quantity;
    public int Cost;
    public int Revenue;
    public int ExpectedProfit;
    public float Minutes;
    public string Fight = string.Empty;
    public int Loot;
    /// <summary>True when the good was sold to a town that has it outside its profile (it only buys it).</summary>
    public bool ToOffProfile;
    /// <summary>The band fought on the way (empty when none).</summary>
    public string Band = string.Empty;
    /// <summary>True when a band was met and the merchant went around it.</summary>
    public bool Detour;

    /// <summary>What the trade alone earned (the sale minus the purchase), fights and replacements apart.</summary>
    public int Profit => Revenue - Cost;
}

/// <summary>How the price and the stock of one good in one town went over the run.</summary>
public class PriceSeries
{
    public string Town;
    public GoodDefinition Good;
    public bool InProfile;
    public int Samples;
    public long MidSum;
    public int MidMin = int.MaxValue;
    public int MidMax;
    public int DaysAtFloor;
    public int DaysAtCeiling;
    public int DaysZeroStock;
    public int StockMin = int.MaxValue;
    public int StockMax;

    public double MidMean => Samples > 0 ? (double)MidSum / Samples : 0.0;
}

/// <summary>What was seen of one route (a good carried from a town to another) each time the merchant judged it.</summary>
public class RouteStats
{
    public string From;
    public string To;
    public GoodDefinition Good;
    public int Evaluations;
    public int Profitable;
    public int BestExpectedProfit;
}

/// <summary>What a simulated run includes: the mechanics can be switched off one by one to measure each one's effect.</summary>
public struct ScenarioOptions
{
    public SimulationStrategy Strategy;
    public bool Combat;
    public bool Wages;
    public bool Healing;
    public bool Recruits;
    /// <summary>Days of the run (0 = the settings' days).</summary>
    public int Days;
    /// <summary>The run starts with this loot (rolled with the seed): its U.V. are the capital, its goods are in the cargo.</summary>
    public LootTable StartingLoot;
    /// <summary>Chance that going around a band fails (negative = the settings' chance): the sensitivity runs change it.</summary>
    public float AvoidFailure;

    public static ScenarioOptions Trade(SimulationStrategy strategy)
    {
        return new ScenarioOptions { Strategy = strategy, Wages = true, AvoidFailure = -1f };
    }

    public static ScenarioOptions FullLoop()
    {
        return new ScenarioOptions { Strategy = SimulationStrategy.Foresight, Combat = true, Wages = true, Healing = true, Recruits = true, AvoidFailure = -1f };
    }
}

/// <summary>All the runs of one report, by role.</summary>
public class SimulationRun
{
    /// <summary>Every run of the first table of the report, in order.</summary>
    public readonly List<ScenarioResult> Scenarios = new List<ScenarioResult>();
    public ScenarioResult Greedy;
    public ScenarioResult Reference;
    public ScenarioResult Replay;
    public ScenarioResult Loop;
    public ScenarioResult LoopReplay;
    public readonly List<ScenarioResult> OtherSeeds = new List<ScenarioResult>();
    public ScenarioResult Start;
    public ScenarioResult StartWithGoods;
    /// <summary>The real start (0 U.V. plus the thief's loot) on every seed.</summary>
    public readonly List<ScenarioResult> StarterSeeds = new List<ScenarioResult>();
    public ScenarioResult LoopNoHospital;
    public ScenarioResult LoopNoRecruits;
    public ScenarioResult LoopNoWages;
    public ScenarioResult ReferenceNoWages;
    public ScenarioResult ReferenceLong;
    public ScenarioResult LoopLong;
    /// <summary>The full loop on every seed again, for each other chance that going around a band fails.</summary>
    public readonly List<KeyValuePair<float, List<ScenarioResult>>> Sensitivity = new List<KeyValuePair<float, List<ScenarioResult>>>();
    public readonly List<LootTable> BandTables = new List<LootTable>();
    /// <summary>The measured fights the runs used (null: the simple model only).</summary>
    public CombatOddsTable Odds;
    public int RecruitPrice;
    public string RecruitName = string.Empty;
    public string StartingParty = string.Empty;

    /// <summary>The full loop over every seed (the main seed first).</summary>
    public List<ScenarioResult> LoopSeeds()
    {
        var all = new List<ScenarioResult> { Loop };
        all.AddRange(OtherSeeds);
        return all;
    }
}

/// <summary>Everything one simulated run measured; the report is made from it.</summary>
public class ScenarioResult
{
    public string Name = string.Empty;
    public ScenarioOptions Options;
    public SimulationStrategy Strategy;
    public bool Combat;
    public int Seed;
    public int StartingCapital;
    /// <summary>The goods of the starting loot, at base price (0 without a starting loot).</summary>
    public long StartingGoodsValue;
    public string StartingLootText = string.Empty;
    public int EndingBalance;
    public int DaysElapsed;
    public int IdleDays;
    public int Victories;
    public int Defeats;
    /// <summary>Bands met on the road.</summary>
    public int Encounters;
    /// <summary>Bands the merchant chose to fight (its chance was at least the threshold).</summary>
    public int FightsChosen;
    /// <summary>Bands it went around.</summary>
    public int Avoided;
    /// <summary>Detours that failed: the fight was forced.</summary>
    public int ForcedFights;
    /// <summary>Fights resolved with the measured table (the others with the fallback model).</summary>
    public int MeasuredFights;
    public int UnitsLost;
    public int LootTotal;
    public int LootLostValue;

    // The ledger: every U.V. that came in or went out, by kind. The wallet must always equal
    // StartingCapital + Sales - Purchases + LootUv - WagesPaid - HealingCost - RecruitCost (checked at each step).
    public long Purchases;
    public long Sales;
    /// <summary>Part of the sales made in town outside a trip (cargo left over: goods of a loot), not in any trip's revenue.</summary>
    public long CargoSoldInTown;
    public long LootUv;
    /// <summary>Wages paid by the simulated payroll (the group of the run, the game's rule).</summary>
    public int WagesPaid;
    /// <summary>Wages charged over the run (after the free days): what was paid plus what is still owed.</summary>
    public long WagesDue;
    /// <summary>Wages owed and not paid at the end of the run.</summary>
    public int EndingDebt;
    /// <summary>The first day of the run (day 1 = the start) on which wages were charged; 0 when none was.</summary>
    public int FirstChargedDay;
    /// <summary>Most the merchant owed at any moment.</summary>
    public int MaxDebt;
    public int HealingCost;
    public int HospitalVisits;
    public int HitPointsHealed;
    public int HitPointsRegenerated;
    public int RecruitCost;
    public int Recruits;
    public int RecruitsRefusedForDebt;
    /// <summary>The group at the end ("Fantassin 40/40, ...").</summary>
    public string EndingParty = string.Empty;
    public int EndingPartySize;
    /// <summary>Goods left in the cargo at the end, at base price (goods of loot that no town on the way bought).</summary>
    public long EndingCargoValue;
    public int StartingPartySize;
    public int RefusedTrades;
    public string EndReason = string.Empty;
    public readonly List<TripRecord> Trips = new List<TripRecord>();
    public readonly List<PriceSeries> Series = new List<PriceSeries>();
    public readonly Dictionary<string, RouteStats> Routes = new Dictionary<string, RouteStats>();
    public readonly List<string> InvariantFailures = new List<string>();
    // The balance at the end of every day, to cut the profit into slices of days.
    public readonly List<int> DailyBalance = new List<int>();

    public int NetProfit => EndingBalance - EndingDebt - StartingCapital;
    public double ProfitPerDay => DaysElapsed > 0 ? (double)NetProfit / DaysElapsed : 0.0;
    /// <summary>What trading earned: sales minus purchases (the sale of the goods found in loot included).</summary>
    public long TradeMargin => Sales - Purchases;
    /// <summary>Income before costs: the trade margin and the U.V. of the loot.</summary>
    public long Income => TradeMargin + LootUv;
    /// <summary>
    /// The share of the loot in the income: the loot gathered (U.V. and goods at base price) over the income. The goods
    /// of the loot are sold with the cargo, so they are counted in the trade margin too: this is an estimate.
    /// </summary>
    public double LootShare => Income > 0 ? Math.Min(1.0, LootTotal / (double)Income) : 0.0;
    /// <summary>The wallet the ledger expects.</summary>
    public long LedgerBalance => StartingCapital + Sales - Purchases + LootUv - WagesPaid - HealingCost - RecruitCost;

    /// <summary>A short text that identifies the sequence of trips, to compare two runs.</summary>
    public string Fingerprint()
    {
        var text = new System.Text.StringBuilder();
        for (int i = 0; i < Trips.Count; i++)
        {
            TripRecord trip = Trips[i];
            text.Append(trip.From).Append('>').Append(trip.To).Append(':').Append(trip.Good.Id).Append('x').Append(trip.Quantity)
                .Append('/').Append(trip.Cost).Append('/').Append(trip.Revenue).Append(';');
        }

        return text.Append(EndingBalance).Append("|w").Append(WagesPaid).Append("|d").Append(EndingDebt).Append("|h").Append(HealingCost)
            .Append("|r").Append(RecruitCost).Append("|v").Append(Victories).Append("|l").Append(Defeats).Append("|p").Append(EndingParty).ToString();
    }
}

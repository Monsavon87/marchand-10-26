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

/// <summary>Everything one simulated run measured; the report is made from it.</summary>
public class ScenarioResult
{
    public string Name = string.Empty;
    public SimulationStrategy Strategy;
    public bool Combat;
    public int Seed;
    public int StartingCapital;
    public int EndingBalance;
    public int DaysElapsed;
    public int IdleDays;
    public int Victories;
    public int Defeats;
    public int UnitsLost;
    public int LootTotal;
    public int LootLostValue;
    public int ReplacementCost;
    public int RefusedTrades;
    public string EndReason = string.Empty;
    public readonly List<TripRecord> Trips = new List<TripRecord>();
    public readonly List<PriceSeries> Series = new List<PriceSeries>();
    public readonly Dictionary<string, RouteStats> Routes = new Dictionary<string, RouteStats>();
    public readonly List<string> InvariantFailures = new List<string>();
    // The balance at the end of every day, to cut the profit into slices of days.
    public readonly List<int> DailyBalance = new List<int>();

    public int NetProfit => EndingBalance - StartingCapital;
    public double ProfitPerDay => DaysElapsed > 0 ? (double)NetProfit / DaysElapsed : 0.0;

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

        return text.Append(EndingBalance).ToString();
    }
}

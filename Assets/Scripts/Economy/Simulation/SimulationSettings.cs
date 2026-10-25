using UnityEngine;

/// <summary>How the simulated merchant chooses its trips.</summary>
public enum SimulationStrategy
{
    /// <summary>Judges a route with the prices of today in the destination town.</summary>
    Greedy,
    /// <summary>Judges a route with the stock the destination town will have when the caravan arrives.</summary>
    Foresight
}

/// <summary>
/// The settings of the economy simulator: how long it runs, the seed of its dice, the money the merchant starts with,
/// the model of the fights on the road, the simulated merchant's rules (reserve, hospital, recruits) and the thresholds
/// the report uses to flag an anomaly and give its verdict. These are hypotheses of the simulator, not game settings.
/// Pure data.
/// </summary>
[CreateAssetMenu(fileName = "SimulationSettings", menuName = "Marchand/Economy/Simulation Settings")]
public class SimulationSettings : ScriptableObject
{
    [Header("Run")]
    [Tooltip("Game days simulated. A trip that began before the end is finished.")]
    [Min(1)] [SerializeField] private int days = 100;
    [Tooltip("Seed of the dice: the same seed always gives the same report.")]
    [SerializeField] private int seed = 1;
    [Tooltip("U.V. the merchant starts with (the real game starts with 0: the first money comes from loot).")]
    [Min(0)] [SerializeField] private int startingCapital = 10000;
    [Tooltip("Days the merchant waits in a town when no trip is worth it, before the run is declared stuck.")]
    [Min(1)] [SerializeField] private int maxIdleDays = 10;
    [Tooltip("Other seeds tried with the fights on, to see how much luck changes the result.")]
    [Min(0)] [SerializeField] private int extraSeeds = 9;

    [Header("Fights on the road")]
    [Tooltip("Chance per game day of travel that the caravan meets an enemy band (while some are left on the map).")]
    [Range(0f, 1f)] [SerializeField] private float encounterChancePerDay = 0.15f;
    [Tooltip("Chance to win a fight with a full group at full health. It is multiplied by the group's hit points over those of a full group.")]
    [Range(0f, 1f)] [SerializeField] private float baseWinChance = 0.75f;
    [Tooltip("A won fight costs each unit a share of its maximum hit points, drawn between this minimum...")]
    [Range(0, 100)] [SerializeField] private int victoryDamageMinPercent = 10;
    [Tooltip("...and this maximum (percent). A unit at 0 dies; a won fight leaves one unit standing.")]
    [Range(0, 100)] [SerializeField] private int victoryDamageMaxPercent = 50;

    [Header("The simulated merchant's rules")]
    [Tooltip("The merchant keeps this many days of wages in the wallet: it never spends them on goods, care or recruits.")]
    [Min(0)] [SerializeField] private int reserveDays = 7;
    [Tooltip("...but a poor merchant must trade to live: with less money than this many times the reserve, it spends all of it on goods.")]
    [Min(1f)] [SerializeField] private float spendAllBelowReserves = 2f;
    [Tooltip("In town, a unit under this share of its maximum hit points goes to the hospital (healed to full), if the reserve stays covered.")]
    [Range(0, 100)] [SerializeField] private int hospitalThresholdPercent = 60;
    [Tooltip("In town (runs with fights), the merchant recruits the cheapest offer up to this size, without debt and keeping the reserve. A full group of this size of the cheapest recruit is the reference strength of the fights.")]
    [Min(1)] [SerializeField] private int targetPartySize = 4;
    [Tooltip("Other chances to win (with a full group) tried on every seed of the full loop, to see how much the survival rests on this hypothesis.")]
    [SerializeField] private float[] sensitivityWinChances = { 0.85f, 0.95f };
    [Tooltip("Days of the long runs, to see whether the profit ever stops growing.")]
    [Min(1)] [SerializeField] private int longRunDays = 300;

    [Header("Verdict thresholds")]
    [Tooltip("The profit is bounded when the profit of the last slice of days stays under this many times the one of the second slice.")]
    [Min(1f)] [SerializeField] private float boundedGrowthRatio = 1.5f;
    [Tooltip("The loot weighs without dominating when its share of the income is at least this...")]
    [Range(0f, 1f)] [SerializeField] private float lootShareMin = 0.10f;
    [Tooltip("...and at most this.")]
    [Range(0f, 1f)] [SerializeField] private float lootShareMax = 0.40f;

    [Header("Anomaly thresholds")]
    [Tooltip("Length in days of the slices the profit is cut into to see whether it grows without end.")]
    [Min(5)] [SerializeField] private int growthWindowDays = 30;
    [Tooltip("A price at a bound (minimum or maximum factor), or a stock at 0, for more than this share of the days is flagged.")]
    [Range(0.05f, 1f)] [SerializeField] private float stuckShare = 0.5f;
    [Tooltip("A single trip that returns more than this share of what it cost (1 = +100 %) is flagged.")]
    [Min(0.1f)] [SerializeField] private float maxTripReturn = 1.5f;

    public int Days => days;
    public int Seed => seed;
    public int StartingCapital => startingCapital;
    public int MaxIdleDays => maxIdleDays;
    public int ExtraSeeds => extraSeeds;
    public float EncounterChancePerDay => encounterChancePerDay;
    public float BaseWinChance => baseWinChance;
    public int VictoryDamageMinPercent => Mathf.Min(victoryDamageMinPercent, victoryDamageMaxPercent);
    public int VictoryDamageMaxPercent => Mathf.Max(victoryDamageMinPercent, victoryDamageMaxPercent);
    public int ReserveDays => reserveDays;
    public float SpendAllBelowReserves => spendAllBelowReserves;
    public int HospitalThresholdPercent => hospitalThresholdPercent;
    public int TargetPartySize => targetPartySize;
    public int LongRunDays => longRunDays;
    public System.Collections.Generic.IReadOnlyList<float> SensitivityWinChances => sensitivityWinChances ?? System.Array.Empty<float>();
    public float BoundedGrowthRatio => boundedGrowthRatio;
    public float LootShareMin => lootShareMin;
    public float LootShareMax => lootShareMax;
    public int GrowthWindowDays => growthWindowDays;
    public float StuckShare => stuckShare;
    public float MaxTripReturn => maxTripReturn;
}

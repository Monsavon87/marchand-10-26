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
/// the model of the fights on the road and the thresholds the report uses to flag an anomaly. Pure data.
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
    [Tooltip("Chance to win a fight with the party at full size. It shrinks with the share of the party that is left.")]
    [Range(0f, 1f)] [SerializeField] private float baseWinChance = 0.75f;
    [Tooltip("Most units a won fight can cost: the number lost is drawn between 0 and this.")]
    [Min(0)] [SerializeField] private int maxLossesOnWin = 2;

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
    public int MaxLossesOnWin => maxLossesOnWin;
    public int GrowthWindowDays => growthWindowDays;
    public float StuckShare => stuckShare;
    public float MaxTripReturn => maxTripReturn;
}

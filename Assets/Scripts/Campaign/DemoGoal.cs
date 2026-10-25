using System;
using UnityEngine;

/// <summary>The player's wealth at some moment: the money and the cargo (goods at their base price).</summary>
public readonly struct DemoScore
{
    public DemoScore(int day, int money, long cargoValue, bool cargoCounted, int debt = 0)
    {
        Day = day;
        Money = money;
        CargoValue = cargoValue;
        CargoCounted = cargoCounted;
        Debt = debt;
    }

    public int Day { get; }
    public int Money { get; }
    public long CargoValue { get; }
    public bool CargoCounted { get; }
    /// <summary>What the caravan still owes (unpaid wages), in U.V.</summary>
    public int Debt { get; }

    /// <summary>The score: the money, plus the cargo when the settings count it, minus the debt.</summary>
    public long Total => Money + (CargoCounted ? CargoValue : 0L) - Debt;
}

/// <summary>
/// The goal of the demo: a number of world days (100). It listens to the campaign calendar, and when the last day is
/// over it holds the map still and raises Finished once, with the player's wealth: the score. It only reads the wallet
/// and the cargo.
/// </summary>
public class DemoGoal : MonoBehaviour
{
    [SerializeField] private DemoGoalSettings settings;
    [SerializeField] private CampaignClock clock;
    [SerializeField] private Wallet wallet;
    [SerializeField] private Inventory inventory;
    [SerializeField] private GoodCatalog catalog;
    [Tooltip("Optional: the map is held still when the demo ends.")]
    [SerializeField] private CampaignPause pause;
    [Tooltip("Optional: the debt of unpaid wages is taken off the final wealth.")]
    [SerializeField] private PartyPayroll payroll;

    /// <summary>Raised once, when the last day is over, with the final wealth.</summary>
    public event Action<DemoScore> Finished = delegate { };

    public bool IsFinished { get; private set; }
    /// <summary>Days the demo lasts.</summary>
    public int Days => settings != null ? settings.Days : 0;

    private void Start()
    {
        if (settings == null || clock == null || wallet == null || inventory == null || catalog == null)
        {
            Debug.LogError("DemoGoal: a serialized reference is not assigned.", this);
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

    /// <summary>The wealth now: the money and the cargo at base price.</summary>
    public DemoScore CurrentScore()
    {
        long cargo = 0;
        for (int i = 0; i < catalog.Goods.Count; i++)
        {
            GoodDefinition good = catalog.Goods[i];
            cargo += (long)good.BasePrice * inventory.GetQuantity(good);
        }

        return new DemoScore(Math.Min(clock.Day, Days), wallet.Balance, cargo, settings.CountCargo, payroll != null ? payroll.Debt : 0);
    }

    // Day 1 is the first day: the demo is over when day (Days + 1) begins.
    private void OnDayStarted(int day)
    {
        if (IsFinished || day <= settings.Days)
        {
            return;
        }

        IsFinished = true;
        if (pause != null)
        {
            pause.SetHeld(this, true);
        }

        Finished(CurrentScore());
    }
}

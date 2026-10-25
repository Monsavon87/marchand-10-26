using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>What one day's payment came to.</summary>
public readonly struct PayrollReport
{
    public PayrollReport(int day, int cost, int debtBefore, int paid, int debtAfter)
    {
        Day = day;
        Cost = cost;
        DebtBefore = debtBefore;
        Paid = paid;
        DebtAfter = debtAfter;
    }

    public int Day { get; }
    /// <summary>What the day cost (all the sources together).</summary>
    public int Cost { get; }
    public int DebtBefore { get; }
    /// <summary>What was really taken from the wallet: the day's cost and the debt, as far as the money went.</summary>
    public int Paid { get; }
    public int DebtAfter { get; }
}

/// <summary>
/// The daily costs of travelling: each world day that begins (the campaign clock raises DayStarted; nothing here has a
/// timer, so nothing is charged while time stands still: pause, a town window, a combat) the costs of every source
/// (the wages of the group, and any source added later) are taken from the wallet, after a few free days at the start.
/// What the wallet cannot pay becomes a debt: it grows, and is paid first from the next day's money, as far as the
/// wallet allows; while it lasts nobody can be recruited (see TownRecruitment). It counts against the final wealth.
/// </summary>
public class PartyPayroll : MonoBehaviour
{
    [SerializeField] private PayrollSettings settings;
    [SerializeField] private CampaignClock clock;
    [SerializeField] private Wallet wallet;
    [SerializeField] private PartyRoster party;

    private readonly List<IDailyCost> sources = new List<IDailyCost>();

    /// <summary>Raised after each payment (not during the free days), with what it came to.</summary>
    public event Action<PayrollReport> Settled = delegate { };

    /// <summary>Raised when the debt changes.</summary>
    public event Action DebtChanged = delegate { };

    /// <summary>What the caravan owes, in U.V.</summary>
    public int Debt { get; private set; }

    public bool DebtBlocksRecruitment => settings != null && settings.DebtBlocksRecruitment;

    /// <summary>The last day of the free period (nothing is charged up to and including it).</summary>
    public int GraceDays => settings != null ? settings.GraceDays : 0;

    public bool InGrace => clock != null && clock.Day <= GraceDays;

    /// <summary>What the group costs each day now, in U.V. (all the sources together).</summary>
    public int DailyCost => CostOf(clock.Day);

    private void Awake()
    {
        sources.Add(new WageCost());
    }

    private void Start()
    {
        if (settings == null || clock == null || wallet == null || party == null)
        {
            Debug.LogError("PartyPayroll: a serialized reference is not assigned.", this);
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

    /// <summary>Extension point: from now on this source is charged each day with the others.</summary>
    public void AddSource(IDailyCost source)
    {
        if (source != null && !sources.Contains(source))
        {
            sources.Add(source);
        }
    }

    public void RemoveSource(IDailyCost source)
    {
        sources.Remove(source);
    }

    /// <summary>Forgives the debt (a new game, a test).</summary>
    public void ClearDebt()
    {
        if (Debt != 0)
        {
            Debt = 0;
            DebtChanged();
        }
    }

    private int CostOf(int day)
    {
        long total = 0;
        for (int i = 0; i < sources.Count; i++)
        {
            total += Math.Max(0, sources[i].CostFor(day, party));
        }

        return (int)Math.Min(total, int.MaxValue);
    }

    private void OnDayStarted(int day)
    {
        if (!PayrollRules.IsCharged(day, settings.GraceDays))
        {
            return;
        }

        int cost = CostOf(day);
        int debtBefore = Debt;
        PayrollRules.Settle(debtBefore, cost, wallet.Balance, out int paid, out int debtAfter);
        if (paid > 0 && wallet.TryWithdraw(paid) != WalletResult.Accepted)
        {
            Debug.LogError("PartyPayroll: the payment of " + paid + " U.V. was refused although the balance was checked.", this);
            debtAfter += paid;
            paid = 0;
        }

        Debt = debtAfter;
        if (Debt != debtBefore)
        {
            DebtChanged();
        }

        if (cost > 0 || debtBefore > 0)
        {
            Settled(new PayrollReport(day, cost, debtBefore, paid, Debt));
        }
    }
}

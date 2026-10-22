using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// How much action time a unit has during one round (real time with pause). Walking costs the time it takes,
/// so slow terrain costs more; future actions will spend from the same pool.
/// Pure data: the combat manager decides, spends and refills it. UnitMover knows nothing about it.
/// </summary>
public class CombatMovementBudget : MonoBehaviour
{
    private static readonly List<CombatMovementBudget> AllBudgets = new List<CombatMovementBudget>();

    [SerializeField] private UnitMover mover;
    [Tooltip("Action time available per round, in seconds. Normally the round duration.")]
    [SerializeField] private float maxTime = 6f;

    /// <summary>Every enabled budget in the scene.</summary>
    public static IReadOnlyList<CombatMovementBudget> All => AllBudgets;

    public UnitMover Mover => mover;
    /// <summary>Action time per round, in seconds.</summary>
    public float Max => maxTime;
    /// <summary>Action time left in the current round, in seconds.</summary>
    public float Remaining { get; private set; }

    // Set by the combat manager: time left before the current round ends.
    public float RoundTimeLeft { get; set; }
    /// <summary>Time this unit can still act before the current round ends.</summary>
    public float UsableTime => Mathf.Min(Remaining, RoundTimeLeft);
    /// <summary>How far the unit can still walk before the current round ends, at its current speed.</summary>
    public float UsableDistance => UsableTime * mover.Speed;

    /// <summary>Raised when the order in progress ended because the unit got to its destination.</summary>
    public event Action OrderCompleted = delegate { };
    /// <summary>Raised when the order in progress was given up because the destination cannot be reached.</summary>
    public event Action OrderFailed = delegate { };

    // Bookkeeping used by the combat manager to measure the distance actually walked.
    public Vector3 LastPosition { get; set; }
    // Where the unit wanted to go (the clicked point): it faces it when the move is cut short.
    public Vector3 FacePoint { get; set; }
    public bool HasFacePoint { get; set; }

    // The order in progress: the clicked destination, kept until the unit gets there (it may take several rounds).
    public bool HasPending { get; private set; }
    public Vector3 PendingDestination { get; private set; }
    // True when the order waits for the next round because no movement is left.
    public bool WaitingForBudget { get; set; }
    // Manager bookkeeping about the walking leg in progress.
    public int LegSerial { get; set; }
    public int LegFrame { get; set; }
    public bool StoppedByBudget { get; set; }

    /// <summary>Starts a new order: the unit will walk toward the destination over as many rounds as needed.</summary>
    public void SetPending(Vector3 destination)
    {
        PendingDestination = destination;
        HasPending = true;
        WaitingForBudget = false;
        StoppedByBudget = false;
    }

    /// <summary>Drops the order in progress without telling anyone (the owner of the order cancelled it).</summary>
    public void ClearPending()
    {
        HasPending = false;
        WaitingForBudget = false;
    }

    /// <summary>Ends the order in progress because the unit arrived.</summary>
    public void CompleteOrder()
    {
        ClearPending();
        OrderCompleted();
    }

    /// <summary>Ends the order in progress because it cannot be carried out.</summary>
    public void FailOrder()
    {
        ClearPending();
        OrderFailed();
    }

    private void Awake()
    {
        if (mover == null)
        {
            Debug.LogError("CombatMovementBudget: mover is not assigned.", this);
            enabled = false;
            return;
        }

        Remaining = maxTime;
        RoundTimeLeft = maxTime;
        LastPosition = transform.position;
    }

    private void OnEnable()
    {
        AllBudgets.Add(this);
    }

    private void OnDisable()
    {
        AllBudgets.Remove(this);
    }

    /// <summary>Gives the full action time back (start of a new round).</summary>
    public void Refill()
    {
        Remaining = maxTime;
    }

    /// <summary>Uses up some action time, never going below zero.</summary>
    public void Spend(float seconds)
    {
        Remaining = Mathf.Max(0f, Remaining - seconds);
    }
}

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// How far a unit may walk during one action period (a "round" in real time with pause).
/// Pure data: the combat manager decides, spends and refills it. UnitMover knows nothing about it.
/// </summary>
public class CombatMovementBudget : MonoBehaviour
{
    private static readonly List<CombatMovementBudget> AllBudgets = new List<CombatMovementBudget>();

    [SerializeField] private UnitMover mover;
    [Tooltip("Maximum walking distance per round, in meters.")]
    [SerializeField] private float maxDistance = 8f;
    [Tooltip("When off, the combat manager neither limits nor tracks this unit (AI units are free for now).")]
    [SerializeField] private bool enforced = true;

    /// <summary>Every enabled budget in the scene.</summary>
    public static IReadOnlyList<CombatMovementBudget> All => AllBudgets;

    public UnitMover Mover => mover;
    public float Max => maxDistance;
    public float Remaining { get; private set; }
    public bool Enforced => enforced;

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

    /// <summary>Drops the order in progress.</summary>
    public void ClearPending()
    {
        HasPending = false;
        WaitingForBudget = false;
    }

    private void Awake()
    {
        if (mover == null)
        {
            Debug.LogError("CombatMovementBudget: mover is not assigned.", this);
            enabled = false;
            return;
        }

        Remaining = maxDistance;
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

    /// <summary>Gives the full distance back (start of a new round).</summary>
    public void Refill()
    {
        Remaining = maxDistance;
    }

    /// <summary>Uses up some distance, never going below zero.</summary>
    public void Spend(float distance)
    {
        Remaining = Mathf.Max(0f, Remaining - distance);
    }
}

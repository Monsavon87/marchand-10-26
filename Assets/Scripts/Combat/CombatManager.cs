using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Real-time combat with pause. It owns the time mode (Space pauses), the action rounds that refill the movement
/// budgets, and it authorizes move orders.
/// A move longer than the movement left is walked in legs: the unit goes as far as its budget allows, then carries
/// on toward the clicked point as soon as it has movement again (next round), until it gets there.
/// UnitMover only executes; this manager decides.
/// While paused the game time is frozen: orders are accepted and the units start walking when play resumes.
/// </summary>
public class CombatManager : MoveOrderGate
{
    // After a leg is requested, the unit has this many frames to start; otherwise the order is given up (no path).
    private const int LegStartFrames = 3;

    [Tooltip("Length of an action round in game seconds. Every movement budget is refilled at the start of a round.")]
    [SerializeField] private float roundDuration = 6f;
    [SerializeField] private Key pauseKey = Key.Space;
    [Tooltip("A walking leg shorter than this is not started: the unit waits for the next round instead.")]
    [SerializeField] private float minOrderDistance = 0.5f;

    private float roundElapsed;

    public bool IsPaused { get; private set; }
    public int RoundNumber { get; private set; } = 1;
    public float RoundTimeRemaining => Mathf.Max(0f, roundDuration - roundElapsed);

    /// <summary>Raised when the game is paused (true) or resumed (false).</summary>
    public event Action<bool> PauseChanged = delegate { };
    /// <summary>Raised at the start of every round, after the budgets were refilled.</summary>
    public event Action RoundStarted = delegate { };

    private void OnEnable()
    {
        Time.timeScale = 1f;
    }

    private void OnDisable()
    {
        // Never leave the game frozen.
        Time.timeScale = 1f;
        IsPaused = false;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard[pauseKey].wasPressedThisFrame)
        {
            SetPaused(!IsPaused);
        }

        if (!IsPaused)
        {
            roundElapsed += Time.deltaTime;
            if (roundElapsed >= roundDuration)
            {
                StartNewRound();
            }
        }

        TrackMovement();
    }

    /// <summary>Pauses or resumes the game.</summary>
    public void SetPaused(bool paused)
    {
        if (IsPaused == paused)
        {
            return;
        }

        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        PauseChanged(paused);
    }

    /// <summary>
    /// Authorizes a move. Units without a movement budget move freely. Otherwise the destination becomes the
    /// unit's order: it walks as far as its movement allows now and carries on in the following rounds until it
    /// arrives. A new order replaces the previous one. Returns false only when there is no path at all.
    /// </summary>
    public override bool RequestMove(UnitMover mover, Vector3 destination)
    {
        CombatMovementBudget budget = FindBudget(mover);
        if (budget == null)
        {
            mover.MoveTo(destination);
            return true;
        }

        if (!mover.TryGetPath(destination, out _, out _))
        {
            return false;
        }

        budget.SetPending(destination);
        BeginLeg(budget);
        return true;
    }

    /// <summary>Cancels the order in progress: the unit stops where it is.</summary>
    public override void CancelOrder(UnitMover mover)
    {
        CombatMovementBudget budget = FindBudget(mover);
        if (budget != null)
        {
            budget.ClearPending();
        }

        mover.Stop();
    }

    /// <summary>The budget of a unit, or null if it has none.</summary>
    public CombatMovementBudget FindBudget(UnitMover mover)
    {
        IReadOnlyList<CombatMovementBudget> all = CombatMovementBudget.All;
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i].Mover == mover)
            {
                return all[i];
            }
        }

        return null;
    }

    // Sends the unit toward its pending destination, as far as the movement left allows.
    private void BeginLeg(CombatMovementBudget budget)
    {
        UnitMover mover = budget.Mover;
        if (budget.Remaining < minOrderDistance)
        {
            // No movement left: the order waits for the next round and the unit stops where it is.
            budget.WaitingForBudget = true;
            if (mover.IsMoving)
            {
                mover.Stop();
            }

            return;
        }

        budget.WaitingForBudget = false;
        budget.StoppedByBudget = false;
        budget.FacePoint = budget.PendingDestination;
        budget.HasFacePoint = true;
        budget.LastPosition = mover.transform.position;
        budget.LegSerial = mover.OrderSerial;
        budget.LegFrame = Time.frameCount;
        mover.MoveTo(budget.PendingDestination, budget.PendingDestination, budget.Remaining);
    }

    private void StartNewRound()
    {
        roundElapsed -= roundDuration;
        RoundNumber++;

        IReadOnlyList<CombatMovementBudget> all = CombatMovementBudget.All;
        for (int i = 0; i < all.Count; i++)
        {
            all[i].Refill();
        }

        RoundStarted();
    }

    // Spends the budget with the distance really walked, stops a unit that used it all up,
    // and chains the walking legs of the orders that need several rounds.
    private void TrackMovement()
    {
        IReadOnlyList<CombatMovementBudget> all = CombatMovementBudget.All;
        for (int i = 0; i < all.Count; i++)
        {
            CombatMovementBudget budget = all[i];
            UnitMover mover = budget.Mover;
            Vector3 position = mover.transform.position;
            if (mover.IsMoving)
            {
                Vector3 step = position - budget.LastPosition;
                step.y = 0f;
                budget.Spend(step.magnitude);

                if (budget.Remaining <= 0f)
                {
                    mover.Stop();
                    budget.StoppedByBudget = true;
                    if (budget.HasFacePoint)
                    {
                        mover.FaceTowards(budget.FacePoint);
                    }
                }
            }

            budget.LastPosition = position;

            if (budget.HasPending)
            {
                UpdatePendingOrder(budget);
            }
        }
    }

    // Decides what an order in progress does once its unit is standing still.
    private void UpdatePendingOrder(CombatMovementBudget budget)
    {
        UnitMover mover = budget.Mover;
        if (mover.IsMoving || mover.IsStarting)
        {
            return;
        }

        if (budget.WaitingForBudget)
        {
            if (budget.Remaining >= minOrderDistance)
            {
                BeginLeg(budget);
            }

            return;
        }

        if (mover.OrderSerial == budget.LegSerial)
        {
            // The leg never started (no path): give the order up once the start-up frames are over.
            if (Time.frameCount > budget.LegFrame + LegStartFrames)
            {
                budget.FailOrder();
            }

            return;
        }

        if (!mover.LastOrderTruncated && !budget.StoppedByBudget)
        {
            // The last leg was not cut and the unit was not stopped for lack of movement: it arrived.
            budget.CompleteOrder();
            return;
        }

        // The unit stopped short: carry on if it still has movement, otherwise wait for the next round.
        if (budget.Remaining >= minOrderDistance)
        {
            BeginLeg(budget);
        }
        else
        {
            budget.WaitingForBudget = true;
        }
    }
}

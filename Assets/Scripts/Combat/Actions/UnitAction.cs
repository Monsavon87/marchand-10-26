using System;
using UnityEngine;

/// <summary>How an action ended.</summary>
public enum ActionResult
{
    /// <summary>The action did what it was asked to do.</summary>
    Succeeded,
    /// <summary>Somebody stopped it (a new request, a death, an encounter...).</summary>
    Cancelled,
    /// <summary>It started but could not be completed (for example the path failed).</summary>
    Impossible
}

/// <summary>The kinds of action a unit can do. Used to give each kind its own recovery time.</summary>
public enum ActionKind
{
    Move,
    Wait,
    Attack
}

/// <summary>What an action is aimed at: a point on the ground, and optionally a unit.</summary>
public readonly struct ActionTarget
{
    public ActionTarget(Vector3 point, Transform target = null)
    {
        Point = point;
        Target = target;
    }

    public Vector3 Point { get; }
    public Transform Target { get; }
}

/// <summary>
/// Something a unit can do in combat. Every action goes through the unit's ActionController, whoever asks
/// (player or AI). An action says whether it can be done (Validate), runs, and reports how it ended (Finished).
/// Time is the common currency: Cost is in seconds of action time.
/// </summary>
public abstract class UnitAction
{
    /// <summary>Name shown in logs and, later, in the interface.</summary>
    public abstract string Name { get; }

    /// <summary>Which kind of action this is.</summary>
    public abstract ActionKind Kind { get; }

    /// <summary>The unit this action is aimed at (attacks), or null.</summary>
    public virtual Transform Target => null;

    /// <summary>Estimated cost in seconds of action time, filled in by a successful Validate.</summary>
    public float Cost { get; protected set; }

    /// <summary>True between Begin and the end of the action.</summary>
    public bool IsRunning { get; private set; }

    /// <summary>Raised once when the action ends, with how it ended.</summary>
    public event Action<UnitAction, ActionResult> Finished = delegate { };

    /// <summary>Can the action be done now on this target? If not, the reason says why (shown to the player).</summary>
    public abstract bool Validate(ActionTarget target, out string reason);

    /// <summary>Starts the action. The caller must have validated it.</summary>
    public void Begin(ActionTarget target)
    {
        IsRunning = true;
        OnBegin(target);
    }

    /// <summary>The action is asked again while it runs, with a new target: it adapts without ending.</summary>
    public abstract void Retarget(ActionTarget target);

    /// <summary>Called every frame while the action runs. Game time: it does not advance while paused.</summary>
    public virtual void Tick(float deltaTime)
    {
    }

    /// <summary>Stops the action. Ends it as Cancelled; does nothing if it is not running.</summary>
    public void Cancel()
    {
        if (!IsRunning)
        {
            return;
        }

        OnCancel();
        End(ActionResult.Cancelled);
    }

    protected abstract void OnBegin(ActionTarget target);

    /// <summary>Undo whatever the action started (for example stop the unit).</summary>
    protected virtual void OnCancel()
    {
    }

    /// <summary>Release what the action holds (event subscriptions). Called on every ending.</summary>
    protected virtual void OnEnd()
    {
    }

    /// <summary>Ends the action. Only the first call counts.</summary>
    protected void End(ActionResult result)
    {
        if (!IsRunning)
        {
            return;
        }

        IsRunning = false;
        OnEnd();
        Finished(this, result);
    }
}

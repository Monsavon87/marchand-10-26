using UnityEngine;

/// <summary>
/// "Poursuivre": walks after a moving unit, following it wherever it goes, until the action is replaced or
/// cancelled (on the campaign map, the encounter with the band stops it) or the target disappears. It only uses
/// the "Déplacer" action, redirected toward the target a few times a second.
/// </summary>
public sealed class ChaseAction : UnitAction
{
    // Seconds between two checks of whether the target moved.
    private const float RepathInterval = 0.3f;
    // The target must have moved this far (m) before the walk is redirected.
    private const float RepathDistance = 1f;

    private readonly MoveAction move;

    private Transform target;
    private float repathTimer;
    private Vector3 lastDestination;

    public ChaseAction(MoveAction move)
    {
        this.move = move;
    }

    public override string Name => "Poursuivre";

    public override ActionKind Kind => ActionKind.Move;

    public override Transform Target => target;

    public override bool Validate(ActionTarget actionTarget, out string reason)
    {
        if (actionTarget.Target == null || !actionTarget.Target.gameObject.activeInHierarchy)
        {
            reason = "aucune cible à poursuivre";
            return false;
        }

        if (!move.Validate(new ActionTarget(actionTarget.Target.position), out reason))
        {
            return false;
        }

        Cost = move.Cost;
        return true;
    }

    protected override void OnBegin(ActionTarget actionTarget)
    {
        target = actionTarget.Target;
        move.Finished += OnMoveFinished;
        Follow();
    }

    public override void Retarget(ActionTarget actionTarget)
    {
        target = actionTarget.Target;
        Follow();
    }

    public override void Tick(float deltaTime)
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            // The target is gone (beaten, or out of the game): nothing left to chase.
            End(ActionResult.Succeeded);
            return;
        }

        repathTimer -= deltaTime;
        if (repathTimer > 0f)
        {
            return;
        }

        repathTimer = RepathInterval;
        Vector3 offset = target.position - lastDestination;
        offset.y = 0f;
        if (!move.IsRunning || offset.sqrMagnitude > RepathDistance * RepathDistance)
        {
            Follow();
        }
    }

    protected override void OnCancel()
    {
        StopWalking();
    }

    protected override void OnEnd()
    {
        move.Finished -= OnMoveFinished;
        StopWalking();
    }

    // Sends the walk to where the target is now.
    private void Follow()
    {
        var destination = new ActionTarget(target.position);
        if (move.IsRunning)
        {
            move.Retarget(destination);
        }
        else
        {
            if (!move.Validate(destination, out _))
            {
                End(ActionResult.Impossible);
                return;
            }

            move.Begin(destination);
        }

        lastDestination = target.position;
        repathTimer = RepathInterval;
    }

    private void StopWalking()
    {
        if (move.IsRunning)
        {
            move.Cancel();
        }
    }

    private void OnMoveFinished(UnitAction action, ActionResult result)
    {
        // Reaching the point is not the end: Tick follows the target again if it moved. An impossible walk is.
        if (result == ActionResult.Impossible)
        {
            End(ActionResult.Impossible);
        }
    }
}

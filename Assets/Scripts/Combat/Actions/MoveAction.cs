using UnityEngine;

/// <summary>
/// "Déplacer": walks the unit to a point and ends when it gets there. It only gives the order (through the unit's
/// order gate when there is one, straight to UnitMover otherwise) and waits for the arrival.
/// A walk costs the time it takes; Cost is an estimate (path length divided by speed).
/// </summary>
public sealed class MoveAction : UnitAction
{
    private readonly UnitMover mover;
    private readonly MoveOrderGate gate;

    public MoveAction(UnitMover mover, MoveOrderGate gate)
    {
        this.mover = mover;
        this.gate = gate;
    }

    public override string Name => "Déplacer";

    public Vector3 Destination { get; private set; }

    public override bool Validate(ActionTarget target, out string reason)
    {
        Vector3 point = target.Point;
        if (float.IsNaN(point.x) || float.IsNaN(point.y) || float.IsNaN(point.z))
        {
            reason = "destination invalide";
            return false;
        }

        // The gate may redirect the point (a click on a town): the check is made on where the unit will really go.
        if (gate != null)
        {
            point = gate.ResolveDestination(point);
        }

        if (!mover.TryGetPath(point, out float length, out _))
        {
            reason = "aucun chemin vers (" + point.x.ToString("F1") + ", " + point.z.ToString("F1") + ")";
            return false;
        }

        Cost = length / Mathf.Max(0.01f, mover.Speed);
        reason = null;
        return true;
    }

    protected override void OnBegin(ActionTarget target)
    {
        mover.Arrived += OnArrived;
        mover.PathFailed += OnPathFailed;
        IssueOrder(target.Point);
    }

    public override void Retarget(ActionTarget target)
    {
        IssueOrder(target.Point);
    }

    protected override void OnCancel()
    {
        if (gate != null)
        {
            gate.CancelOrder(mover);
        }
        else
        {
            mover.Stop();
        }
    }

    protected override void OnEnd()
    {
        mover.Arrived -= OnArrived;
        mover.PathFailed -= OnPathFailed;
    }

    private void IssueOrder(Vector3 destination)
    {
        Destination = destination;
        if (gate == null)
        {
            mover.MoveTo(destination);
        }
        else if (!gate.RequestMove(mover, destination))
        {
            End(ActionResult.Impossible);
        }
    }

    private void OnArrived()
    {
        End(ActionResult.Succeeded);
    }

    private void OnPathFailed(Vector3 destination)
    {
        End(ActionResult.Impossible);
    }
}

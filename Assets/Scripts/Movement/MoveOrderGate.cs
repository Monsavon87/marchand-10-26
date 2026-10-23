using UnityEngine;

/// <summary>
/// Something that authorizes move orders before they reach UnitMover (for example the combat manager,
/// which limits the distance a unit may walk). Controllers that issue orders ask the gate when one is set.
/// </summary>
public abstract class MoveOrderGate : MonoBehaviour
{
    /// <summary>
    /// Asks to send the unit toward the destination. The gate may shorten the move or refuse it.
    /// Returns false if the order was refused.
    /// </summary>
    public abstract bool RequestMove(UnitMover mover, Vector3 destination);

    /// <summary>
    /// True when the gate follows each unit's CombatMovementBudget and uses it to tell when an order really
    /// ended (an order may take several rounds). False when the unit's own arrival is enough.
    /// </summary>
    public virtual bool ReportsThroughBudget => false;

    /// <summary>
    /// The point the unit will really head for when asked to go to 'requested' (a gate may redirect it, for
    /// example a click on a town becomes the town's entrance). Lets a request be checked against the real point.
    /// </summary>
    public virtual Vector3 ResolveDestination(Vector3 requested)
    {
        return requested;
    }

    /// <summary>Drops the order the gate is carrying for this unit and stops the unit where it is.</summary>
    public abstract void CancelOrder(UnitMover mover);
}

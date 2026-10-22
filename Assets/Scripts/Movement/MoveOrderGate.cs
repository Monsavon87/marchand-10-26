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

    /// <summary>Drops the order the gate is carrying for this unit and stops the unit where it is.</summary>
    public abstract void CancelOrder(UnitMover mover);
}

using UnityEngine;

/// <summary>
/// The pace of actions: the short pause a unit takes after each kind of action before it does the next thing.
/// It comes on top of an action's own duration (an attack already lasts its attack time). Pure data.
/// </summary>
[CreateAssetMenu(fileName = "ActionTimingSettings", menuName = "Marchand/Action Timing Settings")]
public class ActionTimingSettings : ScriptableObject
{
    [Tooltip("Seconds of recovery after a strike, and after an attack action ends.")]
    [Min(0f)] [SerializeField] private float attackRecovery = 0.5f;
    [Tooltip("Seconds of recovery after a walk ends, before the next queued order.")]
    [Min(0f)] [SerializeField] private float moveRecovery;
    [Tooltip("Seconds of recovery after a wait ends, before the next queued order.")]
    [Min(0f)] [SerializeField] private float waitRecovery;

    public float RecoveryAfter(ActionKind kind)
    {
        switch (kind)
        {
            case ActionKind.Attack:
                return attackRecovery;
            case ActionKind.Move:
                return moveRecovery;
            default:
                return waitRecovery;
        }
    }
}

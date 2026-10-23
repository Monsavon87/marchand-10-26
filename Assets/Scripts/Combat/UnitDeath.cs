using UnityEngine;

/// <summary>
/// What happens when a unit dies: it stops everything it was doing, drops its order, freezes its AI, and is removed.
/// Removing the object also removes its idle obstacle, so its place on the NavMesh is free again, and it leaves
/// every registry (teams, selection, budgets) by itself. It stops the unit by cancelling its action.
/// </summary>
public class UnitDeath : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private ActionController actions;
    [Tooltip("Optional: the unit's brain, frozen so it gives no more orders.")]
    [SerializeField] private AIUnitController ai;

    private void Start()
    {
        if (health == null || actions == null)
        {
            Debug.LogError("UnitDeath: health or actions is not assigned.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.Died += OnDied;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.Died -= OnDied;
        }
    }

    private void OnDied()
    {
        // Cancelling the action stops the unit and drops its order, through the combat gate when there is one.
        actions.Cancel();
        if (ai != null)
        {
            ai.Interrupt();
        }

        Destroy(gameObject);
    }
}

using UnityEngine;

/// <summary>
/// What happens when a unit dies: it stops everything it was doing, drops its order, freezes its AI, and is removed.
/// Removing the object also removes its idle obstacle, so its place on the NavMesh is free again, and it leaves
/// every registry (teams, selection, budgets) by itself. Uses only UnitMover's public API.
/// </summary>
public class UnitDeath : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private UnitMover mover;
    [Tooltip("Optional: the unit's movement budget, so that an order carried by the combat manager is dropped.")]
    [SerializeField] private CombatMovementBudget budget;
    [Tooltip("Optional: the unit's brain, frozen so it gives no more orders.")]
    [SerializeField] private AIUnitController ai;

    private void Start()
    {
        if (health == null || mover == null)
        {
            Debug.LogError("UnitDeath: health or mover is not assigned.", this);
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
        if (ai != null)
        {
            // Freezes the brain and stops the unit, through the combat gate when there is one.
            ai.Interrupt();
        }
        else
        {
            mover.Stop();
        }

        if (budget != null)
        {
            budget.ClearPending();
        }

        Destroy(gameObject);
    }
}

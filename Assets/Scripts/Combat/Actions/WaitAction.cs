using UnityEngine;

/// <summary>
/// "Attendre": the unit stands still for a given time. It costs exactly that time of the unit's action time,
/// spent as the time passes (so it is frozen by the pause like everything else).
/// </summary>
public sealed class WaitAction : UnitAction
{
    // Not null when the combat manager hands out action time to this unit.
    private readonly CombatMovementBudget budget;
    private float elapsed;

    public WaitAction(CombatMovementBudget budget)
    {
        this.budget = budget;
    }

    public override string Name => "Attendre";

    /// <summary>How long to wait, in seconds. Set before requesting the action.</summary>
    public float Duration { get; set; } = 1f;

    public override bool Validate(ActionTarget target, out string reason)
    {
        if (Duration <= 0f || float.IsNaN(Duration))
        {
            reason = "durée invalide (" + Duration.ToString("F1") + " s)";
            return false;
        }

        if (budget != null && budget.Remaining < Duration)
        {
            reason = "temps d'action insuffisant (reste " + budget.Remaining.ToString("F1") + " s, coût " + Duration.ToString("F1") + " s)";
            return false;
        }

        Cost = Duration;
        reason = null;
        return true;
    }

    protected override void OnBegin(ActionTarget target)
    {
        elapsed = 0f;
    }

    public override void Retarget(ActionTarget target)
    {
        // Asked again while waiting: start the wait over.
        elapsed = 0f;
    }

    public override void Tick(float deltaTime)
    {
        elapsed += deltaTime;
        if (budget != null)
        {
            budget.Spend(deltaTime);
        }

        if (elapsed >= Duration)
        {
            End(ActionResult.Succeeded);
        }
    }
}

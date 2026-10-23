using UnityEngine;

/// <summary>
/// "Attendre": the unit stands still for a given time. It lasts exactly that long, in game time
/// (so it is frozen by the pause like everything else).
/// </summary>
public sealed class WaitAction : UnitAction
{
    private float elapsed;

    public override string Name => "Attendre";

    public override ActionKind Kind => ActionKind.Wait;

    /// <summary>How long to wait, in seconds. Set before requesting the action.</summary>
    public float Duration { get; set; } = 1f;

    public override bool Validate(ActionTarget target, out string reason)
    {
        if (Duration <= 0f || float.IsNaN(Duration))
        {
            reason = "durée invalide (" + Duration.ToString("F1") + " s)";
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
        if (elapsed >= Duration)
        {
            End(ActionResult.Succeeded);
        }
    }
}

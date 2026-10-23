using UnityEngine;

/// <summary>
/// "Attaquer": a melee fight against one enemy until it dies (or the action is replaced or cancelled).
/// Out of reach, the unit walks toward the target with the "Déplacer" action; in reach, it turns to the target and
/// strikes. A strike takes the weapon's attack time, which is also what keeps the strikes from coming in a
/// burst. Each strike is resolved by the combat rules and announced through CombatEvents.
/// </summary>
public sealed class AttackAction : UnitAction
{
    // Seconds between two checks of whether the target moved while the unit walks toward it.
    private const float RepathInterval = 0.3f;
    // The target must have moved this far (m) before the walk is redirected.
    private const float RepathDistance = 0.5f;
    // The unit walks until it is at this fraction of its reach, so it stops safely within range.
    private const float StopAtReachFraction = 0.85f;
    // A unit that is striking only gives up when the target is this much (m) beyond its reach.
    private const float ReachTolerance = 0.2f;
    private const float FaceInterval = 0.25f;

    private readonly UnitStats stats;
    private readonly UnitTeam team;
    private readonly UnitMover mover;
    private readonly MoveAction move;
    private readonly CombatRulesProvider rulesProvider;
    // Optional: gives the recovery after each strike.
    private readonly ActionTimingSettings timing;

    private Transform targetTransform;
    private Health targetHealth;
    private UnitStats targetStats;
    private UnitMover targetMover;
    private ActionController targetActions;
    private bool striking;
    private float strikeElapsed;
    private float repathTimer;
    private float faceTimer;
    private float recoveryLeft;
    private Vector3 lastDestination;

    public AttackAction(UnitStats stats, UnitTeam team, UnitMover mover, MoveAction move,
        CombatRulesProvider rulesProvider, ActionTimingSettings timing)
    {
        this.stats = stats;
        this.team = team;
        this.mover = mover;
        this.move = move;
        this.rulesProvider = rulesProvider;
        this.timing = timing;
    }

    public override string Name => "Attaquer";

    public override ActionKind Kind => ActionKind.Attack;

    /// <summary>The unit being fought, or null.</summary>
    public override Transform Target => targetTransform;

    public override bool Validate(ActionTarget target, out string reason)
    {
        if (!TryResolveTarget(target.Target, out _, out _, out reason))
        {
            return false;
        }

        if (rulesProvider == null || rulesProvider.Rules == null)
        {
            reason = "les règles de combat sont absentes de la scène";
            return false;
        }

        Cost = stats.AttackTime;
        reason = null;
        return true;
    }

    protected override void OnBegin(ActionTarget target)
    {
        SetTarget(target.Target);
        move.Finished += OnMoveFinished;
    }

    public override void Retarget(ActionTarget target)
    {
        if (target.Target != targetTransform)
        {
            SetTarget(target.Target);
        }
    }

    public override void Tick(float deltaTime)
    {
        if (targetHealth == null || targetHealth.IsDead)
        {
            // The target is dead (killed by somebody else, or just now): the fight is won.
            End(ActionResult.Succeeded);
            return;
        }

        Vector3 myPosition = mover.transform.position;
        Vector3 targetPosition = targetTransform.position;
        Vector3 offset = targetPosition - myPosition;
        offset.y = 0f;
        float distance = offset.magnitude;

        float reach = stats.Reach;
        bool inReach = distance <= (striking ? reach + ReachTolerance : reach);
        if (!inReach)
        {
            striking = false;
            strikeElapsed = 0f;
            Approach(myPosition, targetPosition, deltaTime);
            return;
        }

        if (move.IsRunning)
        {
            // In reach: stop walking.
            move.Cancel();
        }

        if (recoveryLeft > 0f)
        {
            // Catching its breath after the last strike.
            recoveryLeft -= deltaTime;
            return;
        }

        if (!striking)
        {
            striking = true;
            strikeElapsed = 0f;
            faceTimer = 0f;
        }

        faceTimer -= deltaTime;
        if (faceTimer <= 0f)
        {
            faceTimer = FaceInterval;
            mover.FaceTowards(targetPosition);
        }

        strikeElapsed += deltaTime;

        if (strikeElapsed >= stats.AttackTime)
        {
            Strike(targetPosition);
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

    // Reads the target's parts and checks that it can be fought.
    private bool TryResolveTarget(Transform target, out Health health, out UnitStats targetUnitStats, out string reason)
    {
        return AttackTargets.TryResolve(target, team.Team, out health, out targetUnitStats, out reason);
    }

    private void SetTarget(Transform target)
    {
        TryResolveTarget(target, out targetHealth, out targetStats, out _);
        targetMover = targetHealth != null ? targetHealth.GetComponent<UnitMover>() : null;
        targetActions = targetHealth != null ? targetHealth.GetComponent<ActionController>() : null;
        targetTransform = targetHealth != null ? targetHealth.transform : null;
        striking = false;
        strikeElapsed = 0f;
        repathTimer = 0f;
        recoveryLeft = 0f;
    }

    // Walks toward the target with the "Déplacer" action and follows it if it moves.
    private void Approach(Vector3 myPosition, Vector3 targetPosition, float deltaTime)
    {
        Vector3 fromTarget = myPosition - targetPosition;
        fromTarget.y = 0f;
        Vector3 direction = fromTarget.sqrMagnitude > 0.0001f ? fromTarget.normalized : Vector3.forward;
        Vector3 destination = targetPosition + direction * (stats.Reach * StopAtReachFraction);

        repathTimer -= deltaTime;
        if (!move.IsRunning)
        {
            var moveTarget = new ActionTarget(destination);
            if (!move.Validate(moveTarget, out _))
            {
                End(ActionResult.Impossible);
                return;
            }

            move.Begin(moveTarget);
            lastDestination = destination;
            repathTimer = RepathInterval;
        }
        else if (repathTimer <= 0f)
        {
            repathTimer = RepathInterval;
            if ((destination - lastDestination).sqrMagnitude > RepathDistance * RepathDistance)
            {
                move.Retarget(new ActionTarget(destination));
                lastDestination = destination;
            }
        }
    }

    // One strike: roll, apply the damage, tell everybody.
    private void Strike(Vector3 targetPosition)
    {
        ICombatRules rules = rulesProvider.Rules;
        // The zone is the one the attacker stands in when the blow lands.
        AttackZone zone = rules.DetermineZone(mover.transform.position, targetTransform);
        AttackResult result = rules.ResolveAttack(stats, targetStats, zone, rules.RollDie());
        if (result.Damage > 0)
        {
            targetHealth.TakeDamage(result.Damage);
        }

        bool killed = targetHealth.IsDead;
        CombatEvents.RaiseAttackResolved(new AttackReport(
            AttackTargets.Describe(stats), AttackTargets.Describe(targetStats), team.Team, result, targetPosition,
            targetHealth.Current, targetStats.MaxHealth, killed));

        striking = false;
        strikeElapsed = 0f;
        recoveryLeft = timing != null ? timing.RecoveryAfter(ActionKind.Attack) : 0f;
        if (killed)
        {
            End(ActionResult.Succeeded);
            return;
        }

        // The one who was attacked reacts: it turns toward the attacker unless it already has a focus of its own
        // (a unit keeps its focus on its first target).
        if (targetActions != null)
        {
            targetActions.NoteAttackedBy(mover.transform);
        }
        else if (targetMover != null)
        {
            targetMover.FaceTowards(mover.transform.position);
        }
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
        // A walk that ends normally is re-issued by Tick if the target is still out of reach.
        // A walk that cannot be done means the target cannot be reached.
        if (result == ActionResult.Impossible)
        {
            End(ActionResult.Impossible);
        }
    }
}

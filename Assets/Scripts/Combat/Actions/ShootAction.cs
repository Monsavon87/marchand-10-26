using UnityEngine;

/// <summary>
/// "Tirer": the ranged attack. Out of range, the unit walks toward the target; in range it turns to it, draws for the
/// weapon's attack time, then looses an arrow at the spot where the target stands. The arrow flies (it does not
/// follow), and the attack is resolved where it lands: on whoever it hit, with the zone and the distance modifier of
/// that moment, announced through CombatEvents like a melee strike.
/// A shot needs a clear line (no wall or pillar between) and a target outside the weapon's dead zone. When it
/// cannot be taken, the unit looks for a better spot to shoot from if the combat is under way (a few tries);
/// otherwise the action is refused or ends as impossible.
/// </summary>
public sealed class ShootAction : UnitAction
{
    private const float RepathInterval = 0.3f;
    private const float RepathDistance = 0.5f;
    // The unit walks until it is at this fraction of its maximum range.
    private const float StopAtRangeFraction = 0.85f;
    // A unit that is drawing only gives up when the target is this much (m) beyond its range.
    private const float RangeTolerance = 0.3f;
    private const float FaceInterval = 0.25f;
    // Seconds between two checks that the shot is still possible.
    private const float ShotCheckInterval = 0.15f;
    // How many times the unit moves to find a better spot before giving up.
    private const int MaxRepositions = 3;

    private readonly UnitStats stats;
    private readonly UnitTeam team;
    private readonly UnitMover mover;
    private readonly MoveAction move;
    private readonly CombatRulesProvider rulesProvider;
    private readonly CombatManager combat;

    private Transform targetTransform;
    private Health targetHealth;
    private UnitStats targetStats;
    private bool drawing;
    private float drawElapsed;
    private bool repositioning;
    private int repositionsDone;
    private float repathTimer;
    private float faceTimer;
    private float shotCheckTimer;
    private float recoveryLeft;
    private Vector3 lastDestination;

    public ShootAction(UnitStats stats, UnitTeam team, UnitMover mover, MoveAction move,
        CombatRulesProvider rulesProvider, CombatManager combat)
    {
        this.stats = stats;
        this.team = team;
        this.mover = mover;
        this.move = move;
        this.rulesProvider = rulesProvider;
        this.combat = combat;
    }

    public override string Name => "Tirer";

    public override ActionKind Kind => ActionKind.Attack;

    public override Transform Target => targetTransform;

    // Moving to a better spot is only tried once the fight has begun.
    private bool IsEngaged => combat != null && combat.State == CombatState.Running;

    public override bool Validate(ActionTarget target, out string reason)
    {
        if (!AttackTargets.TryResolve(target.Target, team.Team, out Health health, out _, out reason))
        {
            return false;
        }

        if (rulesProvider == null || rulesProvider.Rules == null)
        {
            reason = "les règles de combat sont absentes de la scène";
            return false;
        }

        if (combat == null || combat.Settings == null || combat.Settings.ProjectilePrefab == null)
        {
            reason = "les tirs ne sont pas configurés dans la scène";
            return false;
        }

        // Before the fight there is nobody to find a better spot: a shot that cannot be taken from here is refused.
        if (!IsEngaged)
        {
            Vector3 targetPosition = health.transform.position;
            float distance = FlatDistance(mover.transform.position, targetPosition);
            if (distance < stats.DeadZone)
            {
                reason = "la cible est trop proche pour tirer";
                return false;
            }

            if (distance <= stats.MaxRange && !IsShotClear(mover.transform.position, health.transform, out _))
            {
                reason = "la ligne de tir est bloquée";
                return false;
            }
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
            // The target is dead (killed by somebody else, or by an arrow just now): the fight is won.
            End(ActionResult.Succeeded);
            return;
        }

        if (repositioning)
        {
            if (move.IsRunning)
            {
                return;
            }

            // Arrived at the new spot: look again.
            repositioning = false;
            shotCheckTimer = 0f;
        }

        Vector3 myPosition = mover.transform.position;
        Vector3 targetPosition = targetTransform.position;
        float distance = FlatDistance(myPosition, targetPosition);

        if (distance > (drawing ? stats.MaxRange + RangeTolerance : stats.MaxRange))
        {
            drawing = false;
            drawElapsed = 0f;
            Approach(myPosition, targetPosition, deltaTime);
            return;
        }

        if (move.IsRunning)
        {
            // In range: stop walking.
            move.Cancel();
        }

        if (recoveryLeft > 0f)
        {
            // Catching its breath after the last shot.
            recoveryLeft -= deltaTime;
            return;
        }

        shotCheckTimer -= deltaTime;
        if (shotCheckTimer <= 0f)
        {
            shotCheckTimer = ShotCheckInterval;
            if (!CanShoot(myPosition, distance, out string problem))
            {
                OnShotImpossible(myPosition, targetPosition, problem);
                return;
            }
        }

        if (!drawing)
        {
            drawing = true;
            drawElapsed = 0f;
            faceTimer = 0f;
        }

        faceTimer -= deltaTime;
        if (faceTimer <= 0f)
        {
            faceTimer = FaceInterval;
            mover.FaceTowards(targetPosition);
        }

        drawElapsed += deltaTime;
        if (drawElapsed >= stats.AttackTime)
        {
            // The line is checked again at the moment of the release.
            if (!CanShoot(myPosition, distance, out string problem))
            {
                OnShotImpossible(myPosition, targetPosition, problem);
                return;
            }

            Shoot(myPosition, targetPosition);
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

    private void SetTarget(Transform target)
    {
        AttackTargets.TryResolve(target, team.Team, out targetHealth, out targetStats, out _);
        targetTransform = targetHealth != null ? targetHealth.transform : null;
        drawing = false;
        drawElapsed = 0f;
        repositioning = false;
        repositionsDone = 0;
        repathTimer = 0f;
        shotCheckTimer = 0f;
        recoveryLeft = 0f;
    }

    // In range, is the shot possible from here? If not, 'problem' says why (for the journal).
    private bool CanShoot(Vector3 from, float distance, out string problem)
    {
        if (distance < stats.DeadZone)
        {
            problem = "la cible est trop proche";
            return false;
        }

        if (!IsShotClear(from, targetTransform, out _))
        {
            problem = "la ligne de tir est bloquée";
            return false;
        }

        problem = null;
        return true;
    }

    // The shot cannot be taken: find another place to shoot from while the combat runs, else give up.
    private void OnShotImpossible(Vector3 myPosition, Vector3 targetPosition, string problem)
    {
        drawing = false;
        drawElapsed = 0f;
        string who = AttackTargets.Describe(stats);
        if (IsEngaged && repositionsDone < MaxRepositions && TryReposition(myPosition))
        {
            CombatEvents.RaiseNote(who + " change de position : " + problem);
            return;
        }

        CombatEvents.RaiseNote(who + " ne peut pas tirer sur " + AttackTargets.Describe(targetStats) + " : " + problem);
        End(ActionResult.Impossible);
    }

    // True when no wall or pillar lies between the shooter and the target.
    private bool IsShotClear(Vector3 shooterPosition, Transform target, out UnitTeam interceptor)
    {
        CombatSettings settings = combat.Settings;
        Vector3 eye = Vector3.up * settings.EyeHeight;
        return LineOfSight.TraceShot(shooterPosition + eye, target.position + eye, settings.SightBlockers,
            mover.transform, target, team.Team, settings.AllowFriendlyFire, out interceptor);
    }

    // Looks around the target for the nearest reachable place in range, outside the dead zone, with a clear line.
    private bool TryReposition(Vector3 myPosition)
    {
        if (!ShotSpotFinder.TryFind(stats, mover, combat.Settings, team.Team, myPosition, targetTransform, 0f, null, 0f,
                out Vector3 best))
        {
            return false;
        }

        if (move.IsRunning)
        {
            move.Cancel();
        }

        var moveTarget = new ActionTarget(best);
        if (!move.Validate(moveTarget, out _))
        {
            return false;
        }

        move.Begin(moveTarget);
        repositioning = true;
        repositionsDone++;
        return true;
    }

    // Walks toward the target with the "Déplacer" action and follows it if it moves.
    private void Approach(Vector3 myPosition, Vector3 targetPosition, float deltaTime)
    {
        Vector3 fromTarget = myPosition - targetPosition;
        fromTarget.y = 0f;
        Vector3 direction = fromTarget.sqrMagnitude > 0.0001f ? fromTarget.normalized : Vector3.forward;
        Vector3 destination = targetPosition + direction * (stats.MaxRange * StopAtRangeFraction);

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

    // Looses the arrow at the spot where the target stands now.
    private void Shoot(Vector3 myPosition, Vector3 targetPosition)
    {
        CombatSettings settings = combat.Settings;
        Vector3 eye = Vector3.up * settings.EyeHeight;
        Projectile arrow = Object.Instantiate(settings.ProjectilePrefab, myPosition + eye, Quaternion.identity);

        // Everything the resolution needs is taken now: the shooter may be gone by the time the arrow lands.
        string shooterName = AttackTargets.Describe(stats);
        Vector3 firedFrom = myPosition;
        Transform shooter = mover.transform;
        arrow.Launch(targetPosition + eye, settings.ArrowSpeed, settings.ArrowHitRadius, shooter, team.Team,
            settings.AllowFriendlyFire, (victim, point) => OnArrowLanded(victim, point, shooterName, firedFrom, shooter));

        drawing = false;
        drawElapsed = 0f;
        recoveryLeft = combat.Timing != null ? combat.Timing.RecoveryAfter(ActionKind.Attack) : 0f;
    }

    // Resolves the shot where the arrow landed. Not tied to the action still running: the arrow keeps flying.
    private void OnArrowLanded(UnitTeam victim, Vector3 point, string shooterName, Vector3 firedFrom, Transform shooter)
    {
        if (victim == null)
        {
            CombatEvents.RaiseNote("La flèche de " + shooterName + " se perd.");
            return;
        }

        Health health = victim.GetComponent<Health>();
        UnitStats victimStats = victim.GetComponent<UnitStats>();
        ICombatRules rules = rulesProvider != null ? rulesProvider.Rules : null;
        if (health == null || victimStats == null || health.IsDead || rules == null)
        {
            CombatEvents.RaiseNote("La flèche de " + shooterName + " se perd.");
            return;
        }

        Vector3 victimPosition = victim.transform.position;
        float distance = Mathf.Max(0.01f, FlatDistance(firedFrom, victimPosition));
        int rangeModifier = rules.GetRangeModifier(distance, stats.MaxRange);
        // The zone is the one the arrow comes from.
        AttackZone zone = rules.DetermineZone(firedFrom, victim.transform);
        AttackResult result = rules.ResolveAttack(stats, victimStats, zone, rules.RollDie(), rangeModifier, distance);
        if (result.Damage > 0)
        {
            health.TakeDamage(result.Damage);
        }

        bool killed = health.IsDead;
        CombatEvents.RaiseAttackResolved(new AttackReport(
            shooterName, AttackTargets.Describe(victimStats), team.Team, result, victimPosition,
            health.Current, victimStats.MaxHealth, killed));

        // The one who was hit reacts, unless it is on the shooter's side or the shooter is already gone.
        if (!killed && victim.Team != team.Team && shooter != null)
        {
            ActionController victimActions = victim.GetComponent<ActionController>();
            if (victimActions != null)
            {
                victimActions.NoteAttackedBy(shooter);
            }
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
        // A walk that ends normally is handled by Tick. A walk that cannot be done means the target cannot be reached.
        if (result == ActionResult.Impossible)
        {
            End(ActionResult.Impossible);
        }
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }
}

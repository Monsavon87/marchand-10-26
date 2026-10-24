using UnityEngine;

/// <summary>
/// Basic enemy brain: patrols between points, chases a player unit it can SEE come close,
/// and when the target is lost it checks the last seen position, looks around, then resumes its patrol.
/// It acts through the unit's ActionController only, exactly like the player's commands do, so whatever limits
/// the player's actions (the combat manager's action time, for example) limits the AI the same way
/// (the look-around only turns the stationary unit in place).
/// Detection is visual only: a solid object between the eyes and the target blocks it
/// (hearing will be a separate detection source).
/// In a running combat, AICombatBrain takes over (Interrupt) while enemies are known, and hands back with
/// TakeBackControl.
/// </summary>
public class AIUnitController : MonoBehaviour
{
    public enum AIState
    {
        Patrol,
        Chase,
        /// <summary>Sight lost: walking to the last seen position.</summary>
        Investigate,
        /// <summary>Standing still and looking around.</summary>
        Scan,
        /// <summary>Walking back to what it was doing before the chase.</summary>
        ReturnToPatrol
    }

    private const int CircleSegments = 48;
    private const int MaxSightHits = 16;

    // Head sweep: front -> one side -> other side -> front, each leg smoothed.
    private static readonly float[] SweepKeys = { 0f, 1f, -1f, 0f };

    [SerializeField] private ActionController actions;
    [SerializeField] private UnitTeam ownTeam;
    [Tooltip("When on, a chaser fights the unit it chases (the same Attack action as the player). When off, it just closes in.")]
    [SerializeField] private bool attackTargets;

    [Header("Patrol")]
    [Tooltip("Points visited in order, in a loop. Without points the unit stays where it starts.")]
    [SerializeField] private Transform[] patrolPoints;
    [Tooltip("Seconds spent at each patrol point.")]
    [SerializeField] private float waitAtPoint = 1.5f;

    [Header("Detection")]
    [Tooltip("A player unit closer than this, and visible, starts a chase.")]
    [SerializeField] private float detectionRadius = 9f;
    [Tooltip("The chase ends when the target is farther than this. Keep it above the detection radius.")]
    [SerializeField] private float abandonDistance = 15f;
    [Tooltip("The chaser stops this close to a target it can see.")]
    [SerializeField] private float contactDistance = 1.5f;
    [Tooltip("Seconds between two searches for a target, and between two line-of-sight checks while chasing.")]
    [SerializeField] private float scanInterval = 0.2f;
    [Tooltip("Seconds between two refreshes of the chase destination.")]
    [SerializeField] private float chaseRepathInterval = 0.3f;

    [Header("Vision")]
    [Tooltip("Height of the eyes above the unit's pivot, in meters.")]
    [SerializeField] private float eyeHeight = 0.5f;
    [Tooltip("Solid objects on these layers block the line of sight. Units never block it.")]
    [SerializeField] private LayerMask sightBlockers = ~0;

    [Header("Search")]
    [Tooltip("Seconds spent looking around where the target was lost.")]
    [SerializeField] private float searchDuration = 4f;
    [Tooltip("The head sweeps this many degrees to one side, then to the other, then back to the front.")]
    [SerializeField] private float scanAngle = 90f;
    [Tooltip("If the last seen position is not reached within this time, the look-around starts where the unit stands.")]
    [SerializeField] private float investigateTimeout = 6f;

    private readonly RaycastHit[] sightHits = new RaycastHit[MaxSightHits];
    private AIState state;
    private int patrolIndex;
    private Transform target;
    private bool targetVisible;
    private Vector3 lastSeenPosition;
    private Vector3 homePosition;
    private float nextScanTime;
    private float nextSightCheckTime;
    private float nextRepathTime;
    private float resumeTime;
    private bool isWaiting;
    private float investigateStartTime;
    private float scanStartTime;
    private float scanBaseYaw;
    private bool interrupted;

    /// <summary>Current behavior, for debugging.</summary>
    public AIState State => state;

    /// <summary>The points of the patrol route (read-only; empty when the unit has none).</summary>
    public System.Collections.Generic.IReadOnlyList<Transform> PatrolPoints => patrolPoints ?? System.Array.Empty<Transform>();

    /// <summary>
    /// Starts the patrol over from where the unit stands (a band that came back): whatever it was doing is forgotten,
    /// it is no longer interrupted, and it walks to the first point of its route.
    /// </summary>
    public void ResetToPatrol()
    {
        interrupted = false;
        isWaiting = false;
        target = null;
        targetVisible = false;
        state = AIState.Patrol;
        patrolIndex = 0;
        homePosition = transform.position;
        Halt();
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Order(patrolPoints[0].position);
        }
    }

    /// <summary>
    /// Freezes the brain and stops the unit (for example during an encounter on the campaign map).
    /// The behavior is kept: Resume carries on with it.
    /// </summary>
    public void Interrupt()
    {
        interrupted = true;
        Halt();
    }

    /// <summary>
    /// Ends an Interrupt after a combat fought by AICombatBrain: the unit looks around where it stands, then goes back
    /// to its patrol.
    /// </summary>
    public void TakeBackControl()
    {
        if (!interrupted)
        {
            return;
        }

        interrupted = false;
        BeginScan(Time.time);
    }

    /// <summary>Ends an Interrupt: the unit goes back to whatever it was doing.</summary>
    public void Resume()
    {
        if (!interrupted)
        {
            return;
        }

        interrupted = false;
        bool hasRoute = patrolPoints != null && patrolPoints.Length > 0;
        switch (state)
        {
            case AIState.Patrol:
                // While waiting at a point, the normal timer sends the unit on.
                if (!isWaiting && hasRoute)
                {
                    Order(patrolPoints[patrolIndex].position);
                }

                break;

            case AIState.Investigate:
                Order(lastSeenPosition);
                break;

            case AIState.ReturnToPatrol:
                Order(hasRoute ? patrolPoints[patrolIndex].position : homePosition);
                break;
        }
    }

    private void Start()
    {
        if (actions == null || ownTeam == null)
        {
            Debug.LogError("AIUnitController: actions or ownTeam is not assigned.", this);
            enabled = false;
            return;
        }

        if (abandonDistance <= detectionRadius)
        {
            Debug.LogWarning("AIUnitController: abandonDistance should be larger than detectionRadius to avoid flip-flopping.", this);
        }

        homePosition = transform.position;
        state = AIState.Patrol;
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Order(patrolPoints[patrolIndex].position);
        }
    }

    private void OnEnable()
    {
        if (actions != null)
        {
            actions.ActionFinished += OnActionFinished;
        }
    }

    private void OnDisable()
    {
        if (actions != null)
        {
            actions.ActionFinished -= OnActionFinished;
        }
    }

    private void Update()
    {
        if (interrupted)
        {
            return;
        }

        float now = Time.time;
        switch (state)
        {
            case AIState.Patrol:
                if (isWaiting && now >= resumeTime)
                {
                    isWaiting = false;
                    GoToNextPatrolPoint();
                }

                ScanForTarget(now);
                break;

            case AIState.Chase:
                UpdateChase(now);
                break;

            case AIState.Investigate:
                if (now - investigateStartTime > investigateTimeout)
                {
                    BeginScan(now);
                }
                else
                {
                    ScanForTarget(now);
                }

                break;

            case AIState.Scan:
                UpdateScan(now);
                break;

            case AIState.ReturnToPatrol:
                ScanForTarget(now);
                break;
        }
    }

    // Looks for the closest player unit that is inside the detection radius AND visible.
    private void ScanForTarget(float now)
    {
        if (now < nextScanTime)
        {
            return;
        }

        nextScanTime = now + scanInterval;

        Transform closest = null;
        float closestDistance = detectionRadius;
        System.Collections.Generic.IReadOnlyList<UnitTeam> units = UnitTeam.All;
        for (int i = 0; i < units.Count; i++)
        {
            UnitTeam other = units[i];
            if (other.Team == ownTeam.Team)
            {
                continue;
            }

            // The cheap distance test comes first; the line of sight is only checked for close units.
            float distance = FlatDistance(other.transform.position);
            if (distance <= closestDistance && CanSee(other.transform))
            {
                closestDistance = distance;
                closest = other.transform;
            }
        }

        if (closest != null)
        {
            StartChase(closest, now);
        }
    }

    private void StartChase(Transform newTarget, float now)
    {
        UnitTeam detected = newTarget.GetComponentInParent<UnitTeam>();
        if (detected != null)
        {
            CombatEvents.RaiseUnitDetected(ownTeam, detected);
        }

        target = newTarget;
        state = AIState.Chase;
        isWaiting = false;
        targetVisible = true;
        lastSeenPosition = newTarget.position;
        nextSightCheckTime = now + scanInterval;
        nextRepathTime = 0f;
    }

    private void UpdateChase(float now)
    {
        if (target == null || !target.gameObject.activeInHierarchy)
        {
            // The target is gone: look around where the unit stands.
            BeginScan(now);
            return;
        }

        if (now >= nextSightCheckTime)
        {
            nextSightCheckTime = now + scanInterval;
            if (CanSee(target))
            {
                targetVisible = true;
                lastSeenPosition = target.position;
            }
            else
            {
                // Sight lost: go where the target was last seen, never to its real position.
                BeginInvestigate(now);
                return;
            }
        }

        float distance = FlatDistance(target.position);
        if (distance > abandonDistance)
        {
            // Too far to follow: look around here, then resume.
            BeginScan(now);
            return;
        }

        if (attackTargets)
        {
            // The attack action walks to the target and fights it; only (re)start it, never interrupt it.
            if (now >= nextRepathTime && !actions.IsAttacking(target))
            {
                nextRepathTime = now + chaseRepathInterval;
                actions.RequestAttack(target);
            }

            return;
        }

        if (distance <= contactDistance)
        {
            // In contact: hold position (combat comes in a later step).
            if (actions.IsBusy)
            {
                Halt();
            }

            return;
        }

        if (now >= nextRepathTime)
        {
            nextRepathTime = now + chaseRepathInterval;
            Order(target.position);
        }
    }

    private void BeginInvestigate(float now)
    {
        target = null;
        targetVisible = false;
        state = AIState.Investigate;
        investigateStartTime = now;
        // The state is set first: an unreachable position triggers OnPathFailed, which starts the scan.
        Order(lastSeenPosition);
    }

    private void BeginScan(float now)
    {
        target = null;
        targetVisible = false;
        isWaiting = false;
        state = AIState.Scan;
        scanStartTime = now;
        scanBaseYaw = transform.eulerAngles.y;
        Halt();
    }

    // Turns the stationary unit left, right, then back to the front, over searchDuration.
    private void UpdateScan(float now)
    {
        float progress = searchDuration > 0f ? (now - scanStartTime) / searchDuration : 1f;
        if (progress >= 1f)
        {
            transform.rotation = Quaternion.Euler(0f, scanBaseYaw, 0f);
            EndScan();
            return;
        }

        float legs = progress * (SweepKeys.Length - 1);
        int leg = Mathf.Min((int)legs, SweepKeys.Length - 2);
        float blend = Mathf.SmoothStep(0f, 1f, legs - leg);
        float offset = Mathf.Lerp(SweepKeys[leg], SweepKeys[leg + 1], blend) * scanAngle;
        transform.rotation = Quaternion.Euler(0f, scanBaseYaw + offset, 0f);

        ScanForTarget(now);
    }

    // Nothing found: resume what the unit was doing before the chase.
    private void EndScan()
    {
        state = AIState.ReturnToPatrol;
        isWaiting = false;

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Order(homePosition);
            return;
        }

        // patrolIndex still holds the point the unit was heading to before the chase.
        Order(patrolPoints[patrolIndex].position);
    }

    // True if no solid, non-unit object lies between this unit's eyes and the target.
    private bool CanSee(Transform other)
    {
        Vector3 eyes = transform.position + Vector3.up * eyeHeight;
        Vector3 toTarget = other.position - eyes;
        float distance = toTarget.magnitude;
        if (distance < 0.01f)
        {
            return true;
        }

        int count = Physics.RaycastNonAlloc(eyes, toTarget / distance, sightHits, distance, sightBlockers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            if (sightHits[i].collider.GetComponentInParent<UnitTeam>() == null)
            {
                return false;
            }
        }

        return true;
    }

    private void GoToNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            return;
        }

        patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
        Order(patrolPoints[patrolIndex].position);
    }

    // Asks the unit to walk to a point. A refused request is treated like an unreachable point.
    private void Order(Vector3 destination)
    {
        if (!actions.RequestMove(destination))
        {
            OnPathFailed(destination);
        }
    }

    // Stops the unit and drops the action in progress.
    private void Halt()
    {
        actions.Cancel();
    }

    // A walk that ends: arriving is the old "Arrived", an impossible one is the old "path failed".
    // A cancelled one was our own doing (a new state, a stop): nothing to react to.
    private void OnActionFinished(UnitAction action, ActionResult result)
    {
        // While interrupted, somebody else (the combat brain, an encounter) gives the orders: not our business.
        if (interrupted || !(action is MoveAction))
        {
            return;
        }

        if (result == ActionResult.Succeeded)
        {
            OnArrived();
        }
        else if (result == ActionResult.Impossible)
        {
            OnPathFailed(((MoveAction)action).Destination);
        }
    }

    private void OnArrived()
    {
        switch (state)
        {
            case AIState.Patrol:
                isWaiting = true;
                resumeTime = Time.time + waitAtPoint;
                break;

            case AIState.Investigate:
                // Reached the last seen position and the target is not in sight: look around.
                BeginScan(Time.time);
                break;

            case AIState.ReturnToPatrol:
                // Back on the route: pause, then carry on with the patrol.
                state = AIState.Patrol;
                isWaiting = true;
                resumeTime = Time.time + waitAtPoint;
                break;
        }
    }

    // An unreachable point is treated like a reached one: wait, then try the next point.
    // The retry happens in Update, never inside this callback, so a route made only of
    // unreachable points cannot loop forever.
    private void OnPathFailed(Vector3 destination)
    {
        switch (state)
        {
            case AIState.Patrol:
                isWaiting = true;
                resumeTime = Time.time + waitAtPoint;
                break;

            case AIState.Investigate:
                BeginScan(Time.time);
                break;

            case AIState.ReturnToPatrol:
                state = AIState.Patrol;
                isWaiting = true;
                resumeTime = Time.time + waitAtPoint;
                break;
        }
    }

    private float FlatDistance(Vector3 point)
    {
        Vector3 offset = point - transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    private void OnDrawGizmos()
    {
        Vector3 center = transform.position;
        DrawCircle(center, detectionRadius, Color.yellow);
        DrawCircle(center, abandonDistance, new Color(1f, 0.5f, 0f));

        if (Application.isPlaying)
        {
            Vector3 eyes = center + Vector3.up * eyeHeight;
            if (state == AIState.Chase && target != null)
            {
                // Line of sight toward the chased target: green = seen, red = blocked.
                Gizmos.color = targetVisible ? Color.green : Color.red;
                Gizmos.DrawLine(eyes, target.position);
            }
            else if (state == AIState.Investigate)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(eyes, lastSeenPosition);
                Gizmos.DrawWireSphere(lastSeenPosition, 0.5f);
            }
            else if (state == AIState.Scan)
            {
                // Where the unit is looking while it scans.
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(eyes, eyes + transform.forward * 4f);
            }
        }

        if (patrolPoints == null)
        {
            return;
        }

        Gizmos.color = Color.cyan;
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null)
            {
                continue;
            }

            Gizmos.DrawSphere(patrolPoints[i].position, 0.35f);
            Transform next = patrolPoints[(i + 1) % patrolPoints.Length];
            if (next != null)
            {
                Gizmos.DrawLine(patrolPoints[i].position, next.position);
            }
        }
    }

    private static void DrawCircle(Vector3 center, float radius, Color color)
    {
        Gizmos.color = color;
        Vector3 previous = center + new Vector3(radius, 0.1f, 0f);
        for (int i = 1; i <= CircleSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / CircleSegments;
            Vector3 next = center + new Vector3(Mathf.Cos(angle) * radius, 0.1f, Mathf.Sin(angle) * radius);
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
}

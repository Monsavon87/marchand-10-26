using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Executes movement for one unit. The only script allowed to touch the NavMeshAgent.
/// It knows nothing about the mouse, the player, the AI or the combat rules: controllers only call its public API.
/// Optionally, a standing unit turns into a carving NavMeshObstacle so that other units walk around it.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class UnitMover : MonoBehaviour
{
    private const int MaxCorners = 64;
    private const int AreaCount = 32;
    // Max horizontal gap between the requested point and the path end to count as "reached".
    private const float ReachTolerance = 0.5f;
    // Idle-obstacle mode: how far the NavMesh may be from the feet for the agent to be switched back on.
    private const float WakeNavMeshTolerance = 0.35f;
    private const float WakeFallbackRadius = 2f;
    private const int WakeFramesBeforeFallback = 3;
    private const float WakeTimeout = 1.5f;
    private const float SettleTimeout = 1f;
    // The unit may stand inside its own carved hole: the closest NavMesh point can be this far.
    private const float OwnHoleSearchRadius = 3f;

    [SerializeField] private NavMeshAgent agent;
    [Tooltip("Max distance used to find the closest NavMesh point when the destination is off the NavMesh.")]
    [SerializeField] private float snapRadius = 5f;
    [Tooltip("Avoidance priority while the unit stands still (99 = lowest): idle units step aside for units still moving. " +
             "While moving, the unit uses the priority set on its NavMeshAgent.")]
    [SerializeField, Range(0, 99)] private int idleAvoidancePriority = 99;

    [Header("Facing")]
    [Tooltip("Speed, in degrees per second, at which the unit turns on the spot.")]
    [SerializeField] private float turnSpeed = 360f;

    [Header("Idle obstacle (optional)")]
    [Tooltip("When on, the unit is a carving NavMeshObstacle while it stands still (other units walk around it) " +
             "and a NavMeshAgent while it moves. Needs the Idle Obstacle below.")]
    [SerializeField] private bool carveWhenIdle;
    [Tooltip("Carving NavMeshObstacle on a CHILD object (never on the agent's own object). Kept disabled while the unit moves.")]
    [SerializeField] private NavMeshObstacle idleObstacle;

    /// <summary>Raised each time a new move order is accepted (including orders that replace a running one).</summary>
    public event Action MoveStarted = delegate { };
    /// <summary>Raised when the unit reaches the end of its path. Not raised if the order is replaced or stopped.</summary>
    public event Action Arrived = delegate { };
    /// <summary>Raised when no path at all can be found for an order. The current movement is left untouched.</summary>
    public event Action<Vector3> PathFailed = delegate { };

    // Scratch buffer shared by path queries (previews and orders).
    private readonly Vector3[] corners = new Vector3[MaxCorners];
    // Corners of the order currently being executed, kept apart so previews cannot overwrite them.
    private readonly Vector3[] activeCorners = new Vector3[MaxCorners];
    // Per-unit area costs, kept here because the agent forgets them while it is switched off.
    private readonly float[] areaCosts = new float[AreaCount];
    private readonly bool[] areaCostSet = new bool[AreaCount];
    private int activeCornerCount;
    private NavMeshPath path;
    private bool isMoving;
    private float baseSpeed;
    private float speedMultiplier = 1f;
    private int movingAvoidancePriority;

    // Facing.
    private bool hasFacingTarget;
    private float targetYaw;
    private bool hasArrivalFacing;
    private Vector3 arrivalFacePoint;

    // Idle-obstacle mode.
    private bool isObstacleMode;
    private bool isWakingUp;
    private bool isSettling;
    private float settleStartTime;
    private int wakeStartFrame;
    private float wakeStartTime;
    private Vector3 pendingDestination;
    private bool pendingHasFacing;
    private Vector3 pendingFacePoint;
    private float pendingMaxLength;

    /// <summary>True while an order is being executed.</summary>
    public bool IsMoving => isMoving;

    /// <summary>Walking speed now, in m/s: the speed set on the agent times the terrain multiplier.</summary>
    public float Speed => baseSpeed * speedMultiplier;

    /// <summary>True while an accepted order is waiting for the agent to start (idle-obstacle mode wake-up).</summary>
    public bool IsStarting => isWakingUp;

    /// <summary>Counts the orders that really started. Lets a caller see whether its order began.</summary>
    public int OrderSerial { get; private set; }

    /// <summary>True if the last order that started was cut at its maximum path length (the unit stops short).</summary>
    public bool LastOrderTruncated { get; private set; }

    /// <summary>True while the unit turns on the spot toward a point.</summary>
    public bool IsTurning => hasFacingTarget;

    /// <summary>True while the unit is a carving obstacle (idle-obstacle mode, agent switched off).</summary>
    public bool IsObstacle => isObstacleMode && !isWakingUp;

    /// <summary>Position of the unit on the NavMesh surface (feet level, unlike the transform).</summary>
    public Vector3 GroundPosition => FeetPosition();

    private bool UsesIdleObstacle => carveWhenIdle && idleObstacle != null;

    private void Awake()
    {
        path = new NavMeshPath();
        if (agent == null)
        {
            Debug.LogError("UnitMover: agent is not assigned.", this);
            enabled = false;
            return;
        }

        baseSpeed = agent.speed;
        // The priority set on the agent is the one used while moving; the unit starts idle.
        movingAvoidancePriority = agent.avoidancePriority;
        agent.avoidancePriority = idleAvoidancePriority;

        for (int i = 0; i < AreaCount; i++)
        {
            areaCosts[i] = NavMesh.GetAreaCost(i);
        }

        if (idleObstacle != null)
        {
            idleObstacle.carving = true;
            idleObstacle.carveOnlyStationary = false;
            idleObstacle.enabled = false;
        }
    }

    private void Start()
    {
        // The unit starts standing still: it becomes an obstacle right away (unless an order was already given).
        if (UsesIdleObstacle && !isMoving && !isWakingUp)
        {
            EnterObstacleMode();
        }
    }

    private void Update()
    {
        UpdateTurning();

        if (isWakingUp)
        {
            UpdateWakeUp();
            return;
        }

        if (isSettling)
        {
            UpdateSettling();
            return;
        }

        if (!isMoving || agent.pathPending)
        {
            return;
        }

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            OnArrival();
        }
    }

    /// <summary>
    /// Sends the unit toward the destination. If the destination cannot be reached,
    /// the unit goes to the closest reachable point. A new order replaces the current one.
    /// </summary>
    public void MoveTo(Vector3 destination)
    {
        Order(destination, false, default, float.PositiveInfinity);
    }

    /// <summary>
    /// Same as MoveTo, and when the unit arrives it turns on the spot toward 'faceTowardsAtArrival'
    /// (for example the point that was really asked for when the move was cut short).
    /// </summary>
    public void MoveTo(Vector3 destination, Vector3 faceTowardsAtArrival)
    {
        Order(destination, true, faceTowardsAtArrival, float.PositiveInfinity);
    }

    /// <summary>
    /// Same as above, but the unit never walks farther than 'maxPathLength': a longer path is cut there and the
    /// unit stops at the cut point. The cut is made when the unit actually starts, on its real path.
    /// Who decides the limit (a combat manager, for example) is not this class's business.
    /// </summary>
    public void MoveTo(Vector3 destination, Vector3 faceTowardsAtArrival, float maxPathLength)
    {
        Order(destination, true, faceTowardsAtArrival, maxPathLength);
    }

    /// <summary>Stops the unit immediately. Does not raise Arrived.</summary>
    public void Stop()
    {
        if (agent == null)
        {
            return;
        }

        if (isWakingUp)
        {
            // The order was given but the unit had not woken up yet: cancel it and stay an obstacle.
            isWakingUp = false;
            idleObstacle.enabled = true;
            return;
        }

        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }

        isMoving = false;
        hasArrivalFacing = false;
        agent.avoidancePriority = idleAvoidancePriority;
        BeginSettling();
    }

    /// <summary>
    /// Turns the unit on the spot toward a point, without moving. Ignored while the unit moves
    /// (it then faces its direction of travel) and when the point is right on the unit.
    /// </summary>
    public void FaceTowards(Vector3 point)
    {
        Vector3 direction = point - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        targetYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        hasFacingTarget = true;
    }

    /// <summary>
    /// Scales the movement speed (1 = the speed set on the agent at startup). Used for terrain effects.
    /// </summary>
    public void SetSpeedMultiplier(float multiplier)
    {
        if (agent == null)
        {
            return;
        }

        speedMultiplier = Mathf.Max(0.01f, multiplier);
        agent.speed = baseSpeed * speedMultiplier;
    }

    /// <summary>
    /// Sets the path cost of a NavMesh area for this unit only. Takes effect on the next order:
    /// the path of an order already running is not recomputed.
    /// </summary>
    public void SetAreaCost(int areaIndex, float cost)
    {
        if (agent == null || areaIndex < 0 || areaIndex >= AreaCount)
        {
            return;
        }

        areaCosts[areaIndex] = cost;
        areaCostSet[areaIndex] = true;
        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.SetAreaCost(areaIndex, cost);
        }
    }

    /// <summary>Same as SetAreaCost(int, float), using the area name defined in the NavMesh areas.</summary>
    public void SetAreaCost(string areaName, float cost)
    {
        int areaIndex = NavMesh.GetAreaFromName(areaName);
        if (areaIndex < 0)
        {
            Debug.LogError("UnitMover: unknown NavMesh area '" + areaName + "'.", this);
            return;
        }

        SetAreaCost(areaIndex, cost);
    }

    /// <summary>Returns the path cost of a NavMesh area for this unit.</summary>
    public float GetAreaCost(int areaIndex)
    {
        return areaIndex >= 0 && areaIndex < AreaCount ? areaCosts[areaIndex] : 1f;
    }

    /// <summary>
    /// Computes a path to the destination without moving the unit (used for previews).
    /// Returns false if no path exists. If the destination is unreachable, the path leads to the
    /// closest reachable point: endPoint tells where the unit would really stop.
    /// </summary>
    public bool TryGetPath(Vector3 destination, out float length, out Vector3 endPoint)
    {
        length = 0f;
        endPoint = destination;

        if (!ComputePath(destination, out int count, out _))
        {
            return false;
        }

        for (int i = 1; i < count; i++)
        {
            length += Vector3.Distance(corners[i - 1], corners[i]);
        }

        endPoint = corners[count - 1];
        return true;
    }

    /// <summary>
    /// Same computation as TryGetPath, but copies the path corners into the buffer (no allocation).
    /// reachesDestination is false when the unit would stop elsewhere (closest reachable point).
    /// </summary>
    public bool TryGetPathCorners(Vector3 destination, Vector3[] buffer, out int cornerCount, out bool reachesDestination)
    {
        cornerCount = 0;
        reachesDestination = false;

        if (!ComputePath(destination, out int count, out bool isComplete))
        {
            return false;
        }

        cornerCount = Mathf.Min(count, buffer.Length);
        Array.Copy(corners, buffer, cornerCount);

        Vector3 end = corners[count - 1];
        float dx = end.x - destination.x;
        float dz = end.z - destination.z;
        reachesDestination = isComplete && dx * dx + dz * dz <= ReachTolerance * ReachTolerance;
        return true;
    }

    /// <summary>
    /// Same as TryGetPathCorners, but the path starts at 'start' instead of at the unit (for example the end of
    /// the orders the unit has lined up). It uses this unit's agent type and area costs; the unit itself does not
    /// move and the agent is not touched.
    /// </summary>
    public bool TryGetPathCornersFrom(Vector3 start, Vector3 destination, Vector3[] buffer, out int cornerCount, out bool reachesDestination)
    {
        cornerCount = 0;
        reachesDestination = false;
        if (agent == null)
        {
            return false;
        }

        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = agent.agentTypeID,
            areaMask = agent.areaMask
        };
        if (!NavMesh.SamplePosition(start, out NavMeshHit startHit, snapRadius, filter)
            || !NavMesh.SamplePosition(destination, out NavMeshHit destinationHit, snapRadius, filter))
        {
            return false;
        }

        for (int i = 0; i < AreaCount; i++)
        {
            filter.SetAreaCost(i, areaCosts[i]);
        }

        if (!NavMesh.CalculatePath(startHit.position, destinationHit.position, filter, path)
            || path.status == NavMeshPathStatus.PathInvalid)
        {
            return false;
        }

        int count = path.GetCornersNonAlloc(corners);
        if (count <= 0)
        {
            return false;
        }

        cornerCount = Mathf.Min(count, buffer.Length);
        Array.Copy(corners, buffer, cornerCount);

        Vector3 end = corners[count - 1];
        float dx = end.x - destination.x;
        float dz = end.z - destination.z;
        reachesDestination = path.status == NavMeshPathStatus.PathComplete && dx * dx + dz * dz <= ReachTolerance * ReachTolerance;
        return true;
    }

    /// <summary>
    /// Fills the buffer with the unit's current position followed by the corners still to reach.
    /// Returns the number of points written (0 if the unit is not moving). No allocation.
    /// </summary>
    public int GetRemainingPath(Vector3[] buffer)
    {
        if (agent == null || !isMoving || buffer.Length == 0 || activeCornerCount == 0)
        {
            return 0;
        }

        // The corner the agent is heading to is the first one still to reach.
        Vector3 target = agent.steeringTarget;
        int first = activeCornerCount - 1;
        float best = float.MaxValue;
        for (int i = Mathf.Min(1, activeCornerCount - 1); i < activeCornerCount; i++)
        {
            float distance = (activeCorners[i] - target).sqrMagnitude;
            if (distance < best)
            {
                best = distance;
                first = i;
            }
        }

        int written = 0;
        buffer[written++] = agent.transform.position;
        for (int i = first; i < activeCornerCount && written < buffer.Length; i++)
        {
            buffer[written++] = activeCorners[i];
        }

        return written;
    }

    // Routes an order: directly when the agent is on, through a wake-up when the unit is an obstacle.
    private void Order(Vector3 destination, bool hasFacing, Vector3 facePoint, float maxLength)
    {
        if (isObstacleMode || isWakingUp)
        {
            // The agent is off: validate the order with the static path query, then switch the agent on.
            if (!ComputePath(destination, out _, out _))
            {
                PathFailed(destination);
                return;
            }

            pendingDestination = destination;
            pendingHasFacing = hasFacing;
            pendingFacePoint = facePoint;
            pendingMaxLength = maxLength;
            if (!isWakingUp)
            {
                BeginWakeUp();
            }

            return;
        }

        isSettling = false;
        ExecuteOrder(destination, hasFacing, facePoint, maxLength);
    }

    // The agent is on the NavMesh: compute the path (cut at maxLength if needed) and start walking.
    private void ExecuteOrder(Vector3 destination, bool hasFacing, Vector3 facePoint, float maxLength)
    {
        if (!ComputePath(destination, out int count, out _))
        {
            PathFailed(destination);
            return;
        }

        bool cut = false;
        if (maxLength < float.PositiveInfinity && PathMath.Length(corners, count) > maxLength)
        {
            // Too long: walk only as far as allowed, along the same route.
            cut = true;
            count = PathMath.TruncateAt(corners, count, maxLength);
            if (!agent.CalculatePath(corners[count - 1], path) || path.status == NavMeshPathStatus.PathInvalid)
            {
                PathFailed(destination);
                return;
            }

            count = path.GetCornersNonAlloc(corners);
        }

        if (!agent.SetPath(path))
        {
            PathFailed(destination);
            return;
        }

        Array.Copy(corners, activeCorners, count);
        activeCornerCount = count;
        hasFacingTarget = false;
        hasArrivalFacing = hasFacing;
        arrivalFacePoint = facePoint;
        LastOrderTruncated = cut;
        OrderSerial++;
        isMoving = true;
        agent.avoidancePriority = movingAvoidancePriority;
        MoveStarted();
    }

    private void OnArrival()
    {
        isMoving = false;
        agent.avoidancePriority = idleAvoidancePriority;

        if (hasArrivalFacing)
        {
            hasArrivalFacing = false;
            FaceTowards(arrivalFacePoint);
        }

        // Handlers may give a new order right away.
        Arrived();
        if (!isMoving && !isWakingUp)
        {
            BeginSettling();
        }
    }

    // Turns on the spot once the agent has really stopped.
    private void UpdateTurning()
    {
        if (!hasFacingTarget || isMoving || isWakingUp)
        {
            return;
        }

        if (agent.enabled && agent.velocity.sqrMagnitude > 0.01f)
        {
            return;
        }

        float yaw = transform.eulerAngles.y;
        float next = Mathf.MoveTowardsAngle(yaw, targetYaw, turnSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, next, 0f);
        if (Mathf.Abs(Mathf.DeltaAngle(next, targetYaw)) < 0.5f)
        {
            hasFacingTarget = false;
        }
    }

    // ---- Idle-obstacle mode ----

    // After a stop or an arrival, wait for the agent to stand still, then become an obstacle.
    private void BeginSettling()
    {
        if (!UsesIdleObstacle || isObstacleMode)
        {
            return;
        }

        isSettling = true;
        settleStartTime = Time.unscaledTime;
    }

    private void UpdateSettling()
    {
        if (isMoving)
        {
            isSettling = false;
            return;
        }

        bool stopped = !agent.enabled || agent.velocity.sqrMagnitude < 0.04f;
        if (stopped || Time.unscaledTime - settleStartTime > SettleTimeout)
        {
            isSettling = false;
            EnterObstacleMode();
        }
    }

    // Agent off, carving obstacle on: other units now path around this one.
    private void EnterObstacleMode()
    {
        if (isObstacleMode)
        {
            return;
        }

        if (agent.enabled && agent.isOnNavMesh)
        {
            agent.ResetPath();
        }

        isObstacleMode = true;
        isSettling = false;
        agent.enabled = false;
        idleObstacle.enabled = true;
    }

    // Obstacle off first: the NavMesh needs a few frames to fill the hole under the unit.
    private void BeginWakeUp()
    {
        isWakingUp = true;
        wakeStartFrame = Time.frameCount;
        wakeStartTime = Time.unscaledTime;
        idleObstacle.enabled = false;
    }

    private void UpdateWakeUp()
    {
        if (Time.frameCount <= wakeStartFrame + 1)
        {
            return;
        }

        Vector3 feet = FeetPosition();
        NavMeshHit hit = default;
        bool onMesh = NavMesh.SamplePosition(feet, out hit, WakeNavMeshTolerance, NavMesh.AllAreas);
        // Standing inside a neighbour's hole: after a few frames, step out to the closest NavMesh point.
        bool fallback = !onMesh && Time.frameCount > wakeStartFrame + WakeFramesBeforeFallback
                        && NavMesh.SamplePosition(feet, out hit, WakeFallbackRadius, NavMesh.AllAreas);

        if (onMesh || fallback)
        {
            agent.enabled = true;
            isObstacleMode = false;
            isWakingUp = false;
            if (fallback)
            {
                agent.Warp(hit.position);
            }

            for (int i = 0; i < AreaCount; i++)
            {
                if (areaCostSet[i])
                {
                    agent.SetAreaCost(i, areaCosts[i]);
                }
            }

            ExecuteOrder(pendingDestination, pendingHasFacing, pendingFacePoint, pendingMaxLength);
            return;
        }

        if (Time.unscaledTime - wakeStartTime > WakeTimeout)
        {
            // The NavMesh never came back under the unit: stay an obstacle and report the failure.
            isWakingUp = false;
            idleObstacle.enabled = true;
            PathFailed(pendingDestination);
        }
    }

    // Feet level: the NavMesh position of the agent, or the transform minus the base offset when the agent is off.
    private Vector3 FeetPosition()
    {
        return agent.enabled ? agent.nextPosition : transform.position - Vector3.up * agent.baseOffset;
    }

    // Snaps the destination to the NavMesh, computes the path and fills the corners buffer.
    private bool ComputePath(Vector3 destination, out int cornerCount, out bool isComplete)
    {
        cornerCount = 0;
        isComplete = false;

        if (agent == null)
        {
            return false;
        }

        NavMeshQueryFilter filter = new NavMeshQueryFilter
        {
            agentTypeID = agent.agentTypeID,
            areaMask = agent.areaMask
        };
        if (!NavMesh.SamplePosition(destination, out NavMeshHit hit, snapRadius, filter))
        {
            return false;
        }

        if (agent.enabled && agent.isOnNavMesh)
        {
            if (!agent.CalculatePath(hit.position, path) || path.status == NavMeshPathStatus.PathInvalid)
            {
                return false;
            }
        }
        else if (isObstacleMode || isWakingUp)
        {
            // The agent is off: use the static query with this unit's agent type and costs.
            // The unit may stand inside its own carved hole, so the path starts at the closest NavMesh point
            // (the unit will walk straight out of the hole once it wakes up: this estimate is unbiased).
            if (!NavMesh.SamplePosition(FeetPosition(), out NavMeshHit start, OwnHoleSearchRadius, filter))
            {
                return false;
            }

            for (int i = 0; i < AreaCount; i++)
            {
                filter.SetAreaCost(i, areaCosts[i]);
            }

            if (!NavMesh.CalculatePath(start.position, hit.position, filter, path) || path.status == NavMeshPathStatus.PathInvalid)
            {
                return false;
            }
        }
        else
        {
            Debug.LogError("UnitMover: the unit is not on the NavMesh.", this);
            return false;
        }

        cornerCount = path.GetCornersNonAlloc(corners);
        isComplete = path.status == NavMeshPathStatus.PathComplete;
        return cornerCount > 0;
    }
}

using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Executes movement for one unit. The only script allowed to touch the NavMeshAgent.
/// It knows nothing about the mouse, the player or the AI: controllers only call its public API.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class UnitMover : MonoBehaviour
{
    private const int MaxCorners = 64;
    // Max horizontal gap between the requested point and the path end to count as "reached".
    private const float ReachTolerance = 0.5f;

    [SerializeField] private NavMeshAgent agent;
    [Tooltip("Max distance used to find the closest NavMesh point when the destination is off the NavMesh.")]
    [SerializeField] private float snapRadius = 5f;
    [Tooltip("Avoidance priority while the unit stands still (99 = lowest): idle units step aside for units still moving. " +
             "While moving, the unit uses the priority set on its NavMeshAgent.")]
    [SerializeField, Range(0, 99)] private int idleAvoidancePriority = 99;

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
    private int activeCornerCount;
    private NavMeshPath path;
    private bool isMoving;
    private float baseSpeed;
    private int movingAvoidancePriority;

    public bool IsMoving => isMoving;

    /// <summary>Position of the unit on the NavMesh surface (feet level, unlike the transform).</summary>
    public Vector3 GroundPosition => agent.nextPosition;

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
    }

    private void Update()
    {
        if (!isMoving || agent.pathPending)
        {
            return;
        }

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            isMoving = false;
            agent.avoidancePriority = idleAvoidancePriority;
            Arrived();
        }
    }

    /// <summary>
    /// Sends the unit toward the destination. If the destination cannot be reached,
    /// the unit goes to the closest reachable point. A new order replaces the current one.
    /// </summary>
    public void MoveTo(Vector3 destination)
    {
        if (!ComputePath(destination, out int count, out _) || !agent.SetPath(path))
        {
            PathFailed(destination);
            return;
        }

        Array.Copy(corners, activeCorners, count);
        activeCornerCount = count;
        isMoving = true;
        agent.avoidancePriority = movingAvoidancePriority;
        MoveStarted();
    }

    /// <summary>Stops the unit immediately. Does not raise Arrived.</summary>
    public void Stop()
    {
        if (agent == null)
        {
            return;
        }

        agent.ResetPath();
        isMoving = false;
        agent.avoidancePriority = idleAvoidancePriority;
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

        agent.speed = baseSpeed * Mathf.Max(0.01f, multiplier);
    }

    /// <summary>
    /// Sets the path cost of a NavMesh area for this unit only. Takes effect on the next order:
    /// the path of an order already running is not recomputed.
    /// </summary>
    public void SetAreaCost(int areaIndex, float cost)
    {
        if (agent == null)
        {
            return;
        }

        agent.SetAreaCost(areaIndex, cost);
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
        return agent != null ? agent.GetAreaCost(areaIndex) : 1f;
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

    // Snaps the destination to the NavMesh, computes the path and fills the corners buffer.
    private bool ComputePath(Vector3 destination, out int cornerCount, out bool isComplete)
    {
        cornerCount = 0;
        isComplete = false;

        if (agent == null)
        {
            return false;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError("UnitMover: the unit is not on the NavMesh.", this);
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

        if (!agent.CalculatePath(hit.position, path) || path.status == NavMeshPathStatus.PathInvalid)
        {
            return false;
        }

        cornerCount = path.GetCornersNonAlloc(corners);
        isComplete = path.status == NavMeshPathStatus.PathComplete;
        return cornerCount > 0;
    }
}

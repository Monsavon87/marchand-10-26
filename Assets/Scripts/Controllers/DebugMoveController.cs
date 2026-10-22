using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Test controller: Space sends the unit to the next predefined waypoint through UnitMover.
/// Proves that UnitMover works without the player controller. Also logs the mover events.
/// </summary>
public class DebugMoveController : MonoBehaviour
{
    [SerializeField] private UnitMover mover;
    [SerializeField] private Transform[] waypoints;

    private int nextIndex;

    private void Start()
    {
        if (mover == null || waypoints == null || waypoints.Length == 0)
        {
            Debug.LogError("DebugMoveController: mover or waypoints are not assigned.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (mover == null)
        {
            return;
        }

        mover.MoveStarted += OnMoveStarted;
        mover.Arrived += OnArrived;
        mover.PathFailed += OnPathFailed;
    }

    private void OnDisable()
    {
        if (mover == null)
        {
            return;
        }

        mover.MoveStarted -= OnMoveStarted;
        mover.Arrived -= OnArrived;
        mover.PathFailed -= OnPathFailed;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame)
        {
            GoToNextWaypoint();
        }
    }

    /// <summary>Orders the unit to the next waypoint, looping back to the first one.</summary>
    public void GoToNextWaypoint()
    {
        mover.MoveTo(waypoints[nextIndex].position);
        nextIndex = (nextIndex + 1) % waypoints.Length;
    }

    private void OnMoveStarted() => Debug.Log("DebugMoveController: MoveStarted", this);
    private void OnArrived() => Debug.Log("DebugMoveController: Arrived", this);
    private void OnPathFailed(Vector3 destination) => Debug.LogWarning("DebugMoveController: PathFailed for " + destination, this);

    private void OnDrawGizmos()
    {
        if (waypoints == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        foreach (Transform waypoint in waypoints)
        {
            if (waypoint != null)
            {
                Gizmos.DrawSphere(waypoint.position, 0.4f);
            }
        }
    }
}

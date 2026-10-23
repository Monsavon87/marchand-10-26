using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Shows the player where the unit will go: a preview line toward the hovered point,
/// the remaining path while moving, and a marker at the destination.
/// With Shift held (the order would be lined up, not replace the current one), the preview starts from where the
/// unit will be after the orders it already has, not from where it stands.
/// Read-only: it only queries UnitMover and never gives orders.
/// </summary>
public class PathPreview : MonoBehaviour
{
    private const int MaxPoints = 64;
    private const float MarkerHeight = 0.02f;

    [SerializeField] private UnitMover mover;
    [SerializeField] private Camera viewCamera;
    [SerializeField] private LineRenderer hoverLine;
    [SerializeField] private LineRenderer remainingLine;
    [Tooltip("Marker prefab instance. May be a child of the unit: it is detached at startup so it stays where it is placed.")]
    [SerializeField] private GameObject destinationMarker;
    [Tooltip("Optional: the hover preview is only shown while this unit is selected.")]
    [SerializeField] private SelectableUnit selectable;
    [Tooltip("Optional: with Shift held, the hover preview starts at the end of the unit's queued orders.")]
    [SerializeField] private ActionController actions;
    [Tooltip("Hover line material when the hovered point is reached exactly.")]
    [SerializeField] private Material validMaterial;
    [Tooltip("Hover line material when the unit would stop at the closest reachable point instead.")]
    [SerializeField] private Material partialMaterial;
    [Header("Behavior")]
    [Tooltip("Temporary: until a real selection system exists, the unit counts as selected at start.")]
    [SerializeField] private bool selectedAtStart = true;
    [Tooltip("Minimum time between two path recomputations, in seconds.")]
    [SerializeField] private float refreshInterval = 0.1f;
    [Tooltip("Hovered point must move at least this far (m) to recompute while the unit is idle.")]
    [SerializeField] private float minHoverMove = 0.05f;
    [Tooltip("Height of the lines above the ground, to avoid z-fighting.")]
    [SerializeField] private float lineHeight = 0.05f;

    // Reused buffers: no allocation while refreshing.
    private readonly Vector3[] points = new Vector3[MaxPoints];
    private bool isSelected;
    private bool wasMoving;
    private bool hasHoverPoint;
    private Vector3 lastHoverPoint;
    private bool lastHoverQueueing;
    private Vector3 lastHoverQueueStart;
    private float nextRefreshTime;

    private void Start()
    {
        // A prefab cannot point to a scene camera: fall back to the main camera.
        if (viewCamera == null)
        {
            viewCamera = Camera.main;
        }

        if (mover == null || viewCamera == null || hoverLine == null || remainingLine == null
            || destinationMarker == null || validMaterial == null || partialMaterial == null)
        {
            Debug.LogError("PathPreview: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        // The marker must not follow the unit.
        if (destinationMarker.transform.parent != null)
        {
            destinationMarker.transform.SetParent(null, true);
        }

        isSelected = selectable != null ? selectable.IsSelected : selectedAtStart;
        HideAll();
    }

    private void OnEnable()
    {
        if (mover != null)
        {
            mover.MoveStarted += OnMoveStarted;
            mover.Arrived += OnArrived;
        }

        if (selectable != null)
        {
            selectable.SelectionChanged += OnSelectionChanged;
        }
    }

    private void OnDestroy()
    {
        // The detached marker belongs to this unit.
        if (destinationMarker != null)
        {
            Destroy(destinationMarker);
        }
    }

    private void OnDisable()
    {
        if (mover != null)
        {
            mover.MoveStarted -= OnMoveStarted;
            mover.Arrived -= OnArrived;
        }

        if (selectable != null)
        {
            selectable.SelectionChanged -= OnSelectionChanged;
        }

        if (hoverLine != null && remainingLine != null && destinationMarker != null)
        {
            HideAll();
        }
    }

    private void Update()
    {
        // Unscaled time: the preview must keep working while the game is paused.
        if (Time.unscaledTime < nextRefreshTime)
        {
            return;
        }

        nextRefreshTime = Time.unscaledTime + refreshInterval;
        RefreshRemainingPath();
        RefreshHoverPreview();
    }

    /// <summary>Called by the (future) selection system. Only the hover preview depends on selection.</summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        hasHoverPoint = false;
        if (!selected)
        {
            HideHover();
        }
    }

    private void OnSelectionChanged(SelectableUnit unit, bool selected) => SetSelected(selected);

    private void OnMoveStarted() => RefreshRemainingPath();

    private void OnArrived()
    {
        remainingLine.enabled = false;
        destinationMarker.SetActive(false);
        hasHoverPoint = false;
        wasMoving = false;
    }

    private void RefreshRemainingPath()
    {
        bool moving = mover.IsMoving;
        if (moving != wasMoving)
        {
            wasMoving = moving;
            // The start of the hover preview changed: force a recompute.
            hasHoverPoint = false;
        }

        // Also covers Stop(), which raises no event.
        int count = moving ? mover.GetRemainingPath(points) : 0;
        if (count < 2)
        {
            remainingLine.enabled = false;
            destinationMarker.SetActive(false);
            return;
        }

        destinationMarker.transform.position = points[count - 1] + Vector3.up * MarkerHeight;
        destinationMarker.SetActive(true);
        DrawLine(remainingLine, points, count);
    }

    private void RefreshHoverPreview()
    {
        if (!isSelected)
        {
            HideHover();
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            HideHover();
            return;
        }

        Ray ray = viewCamera.ScreenPointToRay(mouse.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            hasHoverPoint = false;
            HideHover();
            return;
        }

        // Shift held: the new order would be lined up, so the path starts where the last order ends.
        Vector3 queueStart = Vector3.zero;
        bool queueing = IsShiftHeld() && actions != null && actions.TryGetLastOrderPoint(out queueStart);

        // While the unit is idle and nothing changed (cursor, Shift, end of the queue), the preview is still valid.
        if (hasHoverPoint && !mover.IsMoving && queueing == lastHoverQueueing
            && (!queueing || (queueStart - lastHoverQueueStart).sqrMagnitude < minHoverMove * minHoverMove)
            && (hit.point - lastHoverPoint).sqrMagnitude < minHoverMove * minHoverMove)
        {
            return;
        }

        hasHoverPoint = true;
        lastHoverPoint = hit.point;
        lastHoverQueueing = queueing;
        lastHoverQueueStart = queueStart;

        int count;
        bool reachesDestination;
        bool found = queueing
            ? mover.TryGetPathCornersFrom(queueStart, hit.point, points, out count, out reachesDestination)
            : mover.TryGetPathCorners(hit.point, points, out count, out reachesDestination);
        if (!found || count < 2)
        {
            HideHover();
            return;
        }

        Material wanted = reachesDestination ? validMaterial : partialMaterial;
        if (hoverLine.sharedMaterial != wanted)
        {
            hoverLine.sharedMaterial = wanted;
        }

        DrawLine(hoverLine, points, count);
    }

    private static bool IsShiftHeld()
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
    }

    // Writes the first 'count' points of the buffer into the line, slightly above the ground.
    private void DrawLine(LineRenderer line, Vector3[] buffer, int count)
    {
        for (int i = 0; i < count; i++)
        {
            buffer[i].y += lineHeight;
        }

        line.positionCount = count;
        line.SetPositions(buffer);
        line.enabled = true;
    }

    private void HideHover()
    {
        hoverLine.enabled = false;
    }

    private void HideAll()
    {
        HideHover();
        remainingLine.enabled = false;
        destinationMarker.SetActive(false);
    }
}

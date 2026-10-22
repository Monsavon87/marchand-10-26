using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Shows the player where the unit will go: a preview line toward the hovered point,
/// the remaining path while moving, and a marker at the destination.
/// With a movement budget, the preview is green as far as the unit walks before the round ends and red beyond.
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
    [Tooltip("Hover line material when the hovered point is reached exactly.")]
    [SerializeField] private Material validMaterial;
    [Tooltip("Hover line material when the unit would stop at the closest reachable point instead.")]
    [SerializeField] private Material partialMaterial;
    [Header("Movement budget (optional)")]
    [Tooltip("When set, the hover preview is cut at the remaining movement: green inside, red beyond.")]
    [SerializeField] private CombatMovementBudget budget;
    [Tooltip("Line drawn for the part of the path that is out of range (red material).")]
    [SerializeField] private LineRenderer outOfRangeLine;
    [Tooltip("Thin line showing the whole route of an order that takes several rounds; the marker then sits on its final destination.")]
    [SerializeField] private LineRenderer pendingLine;
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
    private readonly Vector3[] inRangePoints = new Vector3[MaxPoints];
    private readonly Vector3[] outOfRangePoints = new Vector3[MaxPoints];
    private readonly Vector3[] planPoints = new Vector3[MaxPoints];
    private bool isSelected;
    private bool wasMoving;
    private bool hasHoverPoint;
    private Vector3 lastHoverPoint;
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
        // An order that goes on over several rounds keeps its marker on the final destination.
        if (!HasPlan)
        {
            destinationMarker.SetActive(false);
        }

        hasHoverPoint = false;
        wasMoving = false;
    }

    // True while the unit has an order in progress that it will carry on in the next rounds.
    private bool HasPlan => budget != null && pendingLine != null && budget.HasPending;

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
        bool drawingLeg = count >= 2;
        if (drawingLeg)
        {
            destinationMarker.transform.position = points[count - 1] + Vector3.up * MarkerHeight;
            destinationMarker.SetActive(true);
            DrawLine(remainingLine, points, count);
        }
        else
        {
            remainingLine.enabled = false;
        }

        // The whole route to the clicked point, until the unit gets there.
        bool drawingPlan = false;
        if (HasPlan && mover.TryGetPathCorners(budget.PendingDestination, planPoints, out int planCount, out _) && planCount >= 2)
        {
            DrawLine(pendingLine, planPoints, planCount);
            destinationMarker.transform.position = planPoints[planCount - 1] + Vector3.up * MarkerHeight;
            destinationMarker.SetActive(true);
            drawingPlan = true;
        }
        else if (pendingLine != null)
        {
            pendingLine.enabled = false;
        }

        if (!drawingLeg && !drawingPlan)
        {
            destinationMarker.SetActive(false);
        }
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

        // While the unit is idle and the cursor has not moved, the preview is still valid.
        if (hasHoverPoint && !mover.IsMoving
            && (hit.point - lastHoverPoint).sqrMagnitude < minHoverMove * minHoverMove)
        {
            return;
        }

        hasHoverPoint = true;
        lastHoverPoint = hit.point;

        if (!mover.TryGetPathCorners(hit.point, points, out int count, out bool reachesDestination) || count < 2)
        {
            HideHover();
            return;
        }

        Material wanted = reachesDestination ? validMaterial : partialMaterial;
        if (hoverLine.sharedMaterial != wanted)
        {
            hoverLine.sharedMaterial = wanted;
        }

        if (budget == null || outOfRangeLine == null)
        {
            DrawLine(hoverLine, points, count);
            return;
        }

        // Green as far as the remaining movement allows, red beyond.
        PathMath.SplitAt(points, count, budget.UsableDistance, inRangePoints, out int inRangeCount, outOfRangePoints, out int outOfRangeCount);
        if (inRangeCount >= 2)
        {
            DrawLine(hoverLine, inRangePoints, inRangeCount);
        }
        else
        {
            hoverLine.enabled = false;
        }

        if (outOfRangeCount >= 2)
        {
            DrawLine(outOfRangeLine, outOfRangePoints, outOfRangeCount);
        }
        else
        {
            outOfRangeLine.enabled = false;
        }
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
        if (outOfRangeLine != null)
        {
            outOfRangeLine.enabled = false;
        }
    }

    private void HideAll()
    {
        HideHover();
        remainingLine.enabled = false;
        if (pendingLine != null)
        {
            pendingLine.enabled = false;
        }

        destinationMarker.SetActive(false);
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Strategy camera: a tilted top-down view orbiting a pivot point on the ground.
/// Keyboard, screen-edge and right-mouse-drag panning, mouse wheel zoom, optional rotation, bounds,
/// and centering on a target.
/// Independent from units and controllers: the only link is a plain Transform to center on.
/// </summary>
[RequireComponent(typeof(Camera))]
public class StrategyCamera : MonoBehaviour
{
    [Header("View")]
    [Tooltip("Tilt of the camera in degrees (90 = straight down).")]
    [SerializeField, Range(10f, 89f)] private float pitch = 55f;
    [Tooltip("Heading of the camera in degrees. Fixed when rotation is disabled.")]
    [SerializeField] private float yaw;
    [Tooltip("Ground point the camera looks at when the game starts. Its Y is the ground height.")]
    [SerializeField] private Vector3 startPivot = Vector3.zero;
    [SerializeField] private float startDistance = 29.3f;

    [Header("Movement")]
    [Tooltip("Panning speed in m/s, at the start distance.")]
    [SerializeField] private float moveSpeed = 20f;
    [Tooltip("Pan faster when zoomed out and slower when zoomed in.")]
    [SerializeField] private bool scaleSpeedWithZoom = true;
    [SerializeField] private bool edgeScrollEnabled = true;
    [Tooltip("Width in pixels of the screen border that triggers edge scrolling.")]
    [SerializeField] private float edgeSize = 20f;
    [Tooltip("Smoothing time of movement, zoom and rotation, in seconds. 0 = instant.")]
    [SerializeField] private float smoothTime = 0.1f;

    [Header("Mouse drag")]
    [Tooltip("Hold the right mouse button and drag: the ground follows the cursor.")]
    [SerializeField] private bool mouseDragEnabled = true;
    [Tooltip("Cursor shown while dragging. Leave empty to use the built-in hand.")]
    [SerializeField] private Texture2D dragCursor;
    [Tooltip("Pixel of the cursor texture (from its top-left corner) that points at the screen.")]
    [SerializeField] private Vector2 dragCursorHotspot = new Vector2(16f, 16f);

    [Header("Mouse rotation")]
    [Tooltip("Hold the middle mouse button and move the mouse horizontally to rotate. Needs 'Allow Rotation'.")]
    [SerializeField] private float mouseRotateSensitivity = 0.25f;
    [Tooltip("Cursor shown while rotating. Leave empty to use the built-in rotation icon.")]
    [SerializeField] private Texture2D rotateCursor;
    [Tooltip("Pixel of the cursor texture (from its top-left corner) that points at the screen.")]
    [SerializeField] private Vector2 rotateCursorHotspot = new Vector2(16f, 16f);

    [Header("Zoom")]
    [SerializeField] private float minDistance = 8f;
    [SerializeField] private float maxDistance = 60f;
    [Tooltip("Fraction of the distance removed or added per mouse wheel notch.")]
    [SerializeField, Range(0.01f, 0.5f)] private float zoomStep = 0.15f;

    [Header("Rotation")]
    [SerializeField] private bool allowRotation = true;
    [Tooltip("Rotation speed in degrees per second.")]
    [SerializeField] private float rotateSpeed = 90f;
    [Tooltip("Keys are physical positions (US layout): Q/E are the A/E keys on an AZERTY keyboard.")]
    [SerializeField] private Key rotateLeftKey = Key.Q;
    [SerializeField] private Key rotateRightKey = Key.E;

    [Header("Bounds")]
    [Tooltip("Area (XZ) the pivot cannot leave. Drawn in the Scene view when the camera is selected.")]
    [SerializeField] private Bounds bounds = new Bounds(Vector3.zero, new Vector3(96f, 0f, 96f));

    [Header("Focus")]
    [Tooltip("Transform the camera centers on when the center key is pressed (set by the selection system later).")]
    [SerializeField] private Transform focusTarget;
    [SerializeField] private Key centerKey = Key.F;

    private Camera viewCamera;
    private Vector3 pivot;
    private Vector3 targetPivot;
    private Vector3 pivotVelocity;
    private float distance;
    private float targetDistance;
    private float distanceVelocity;
    private float currentYaw;
    private float targetYaw;
    private float yawVelocity;
    private bool isCentering;
    // The cursor reads (0,0), a screen corner, until the first mouse event: edge scrolling waits for a real move.
    private bool mouseHasMoved;
    private Vector2 firstMousePosition;
    // Right-mouse drag: the ground point grabbed when the button went down, and the generated hand cursor.
    private bool isDragging;
    private Vector3 dragGroundPoint;
    private Texture2D generatedHandCursor;
    // Middle-mouse rotation, and the generated rotation cursor.
    private bool isRotatingWithMouse;
    private Texture2D generatedRotateCursor;

    /// <summary>True while the player holds the right mouse button to move the view.</summary>
    public bool IsDragging => isDragging;

    /// <summary>True while the player holds the middle mouse button to rotate the view.</summary>
    public bool IsRotating => isRotatingWithMouse;

    /// <summary>Sets the Transform the camera centers on (e.g. the selected unit).</summary>
    public void SetFocusTarget(Transform target)
    {
        focusTarget = target;
        if (target == null)
        {
            isCentering = false;
        }
    }

    private void OnValidate()
    {
        minDistance = Mathf.Max(0.1f, minDistance);
        maxDistance = Mathf.Max(minDistance, maxDistance);
        startDistance = Mathf.Clamp(startDistance, minDistance, maxDistance);
    }

    private void Awake()
    {
        viewCamera = GetComponent<Camera>();
    }

    private void Start()
    {
        pivot = targetPivot = ClampToBounds(startPivot);
        distance = targetDistance = Mathf.Clamp(startDistance, minDistance, maxDistance);
        currentYaw = targetYaw = yaw;
        if (Mouse.current != null)
        {
            firstMousePosition = Mouse.current.position.ReadValue();
        }

        ApplyPose();
    }

    private void OnDisable()
    {
        // Never leave a custom cursor behind.
        if (isDragging || isRotatingWithMouse)
        {
            isDragging = false;
            isRotatingWithMouse = false;
            UpdateCursor();
        }
    }

    private void OnDestroy()
    {
        if (generatedHandCursor != null)
        {
            Destroy(generatedHandCursor);
        }

        if (generatedRotateCursor != null)
        {
            Destroy(generatedRotateCursor);
        }
    }

    private void LateUpdate()
    {
        // Unscaled time: the camera stays usable when the game is paused.
        float deltaTime = Time.unscaledDeltaTime;
        ReadInput(deltaTime);
        Smooth(deltaTime);
        ApplyPose();
    }

    private void ReadInput(float deltaTime)
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (mouse != null && !mouseHasMoved && mouse.position.ReadValue() != firstMousePosition)
        {
            mouseHasMoved = true;
        }

        UpdateDrag(mouse);
        UpdateMouseRotation(mouse);

        // Panning: WASD / arrows + screen edges.
        Vector2 move = Vector2.zero;
        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) move.y += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) move.y -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) move.x += 1f;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) move.x -= 1f;
        }

        // Edge scrolling is suspended while dragging or rotating with the mouse.
        if (edgeScrollEnabled && mouseHasMoved && !isDragging && !isRotatingWithMouse && Application.isFocused)
        {
            move += ReadEdgeInput(mouse.position.ReadValue());
        }

        if (move != Vector2.zero)
        {
            // Manual panning cancels a centering in progress.
            isCentering = false;

            float speed = moveSpeed * (scaleSpeedWithZoom ? targetDistance / startDistance : 1f);
            Vector3 direction = Quaternion.Euler(0f, targetYaw, 0f) * new Vector3(move.x, 0f, move.y);
            if (direction.sqrMagnitude > 1f)
            {
                direction.Normalize();
            }

            targetPivot = ClampToBounds(targetPivot + direction * (speed * deltaTime));
        }

        // Zoom: mouse wheel, multiplicative so each notch feels the same at any distance.
        // Not while the cursor is over the interface: the wheel scrolls the journal there.
        if (mouse != null && !UiPointerGuard.IsPointerOverUi)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (scroll != 0f)
            {
                targetDistance = Mathf.Clamp(targetDistance * (1f - Mathf.Sign(scroll) * zoomStep), minDistance, maxDistance);
            }
        }

        // Rotation (optional).
        if (allowRotation && keyboard != null)
        {
            if (keyboard[rotateLeftKey].isPressed) targetYaw -= rotateSpeed * deltaTime;
            if (keyboard[rotateRightKey].isPressed) targetYaw += rotateSpeed * deltaTime;
        }

        // Centering on the focus target.
        if (keyboard != null && keyboard[centerKey].wasPressedThisFrame && focusTarget != null)
        {
            isCentering = true;
        }

        if (isCentering)
        {
            if (focusTarget == null)
            {
                isCentering = false;
            }
            else
            {
                Vector3 goal = ClampToBounds(focusTarget.position);
                targetPivot = goal;
                if ((pivot - goal).sqrMagnitude < 0.0025f)
                {
                    isCentering = false;
                }
            }
        }
    }

    // Right mouse button held: the ground point grabbed on press stays under the cursor.
    private void UpdateDrag(Mouse mouse)
    {
        if (mouse == null)
        {
            return;
        }

        if (!isDragging)
        {
            if (mouseDragEnabled && Application.isFocused && mouse.rightButton.wasPressedThisFrame)
            {
                BeginDrag(mouse.position.ReadValue());
            }

            return;
        }

        if (!mouseDragEnabled || !Application.isFocused || !mouse.rightButton.isPressed)
        {
            EndDrag();
            return;
        }

        if (!TryGetGroundPoint(mouse.position.ReadValue(), out Vector3 point))
        {
            return;
        }

        // Moving the pivot by d moves the ground point under the cursor by d as well.
        // Applied at once, without smoothing, otherwise the ground would lag behind the cursor.
        pivot = ClampToBounds(pivot + (dragGroundPoint - point));
        targetPivot = pivot;
        pivotVelocity = Vector3.zero;
    }

    private void BeginDrag(Vector2 screenPosition)
    {
        if (!TryGetGroundPoint(screenPosition, out dragGroundPoint))
        {
            return;
        }

        isDragging = true;
        isCentering = false;
        UpdateCursor();
    }

    private void EndDrag()
    {
        isDragging = false;
        UpdateCursor();
    }

    // Middle mouse button held: horizontal mouse movement turns the view around the pivot.
    private void UpdateMouseRotation(Mouse mouse)
    {
        if (mouse == null)
        {
            return;
        }

        if (!isRotatingWithMouse)
        {
            if (allowRotation && Application.isFocused && mouse.middleButton.wasPressedThisFrame)
            {
                isRotatingWithMouse = true;
                UpdateCursor();
            }

            return;
        }

        if (!allowRotation || !Application.isFocused || !mouse.middleButton.isPressed)
        {
            isRotatingWithMouse = false;
            UpdateCursor();
            return;
        }

        // Moving right makes the scene follow the cursor to the right.
        targetYaw -= mouse.delta.ReadValue().x * mouseRotateSensitivity;
    }

    // Shows the cursor of the action in progress (rotation wins over drag), or the normal cursor.
    private void UpdateCursor()
    {
        if (isRotatingWithMouse)
        {
            if (rotateCursor == null && generatedRotateCursor == null)
            {
                generatedRotateCursor = CursorTextures.CreateRotation();
            }

            Cursor.SetCursor(rotateCursor != null ? rotateCursor : generatedRotateCursor, rotateCursorHotspot, CursorMode.Auto);
        }
        else if (isDragging)
        {
            if (dragCursor == null && generatedHandCursor == null)
            {
                generatedHandCursor = CursorTextures.CreateHand();
            }

            Cursor.SetCursor(dragCursor != null ? dragCursor : generatedHandCursor, dragCursorHotspot, CursorMode.Auto);
        }
        else
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }
    }

    // Point of the ground plane under a screen position (false if the ray does not reach the ground).
    private bool TryGetGroundPoint(Vector2 screenPosition, out Vector3 point)
    {
        Ray ray = viewCamera.ScreenPointToRay(screenPosition);
        Plane ground = new Plane(Vector3.up, new Vector3(0f, startPivot.y, 0f));
        if (ground.Raycast(ray, out float enter))
        {
            point = ray.GetPoint(enter);
            return true;
        }

        point = Vector3.zero;
        return false;
    }

    // -1/0/1 per axis depending on the cursor being inside the border of the screen.
    private Vector2 ReadEdgeInput(Vector2 mousePosition)
    {
        if (mousePosition.x < 0f || mousePosition.y < 0f || mousePosition.x > Screen.width || mousePosition.y > Screen.height)
        {
            return Vector2.zero;
        }

        Vector2 edge = Vector2.zero;
        if (mousePosition.x <= edgeSize) edge.x = -1f;
        else if (mousePosition.x >= Screen.width - edgeSize) edge.x = 1f;
        if (mousePosition.y <= edgeSize) edge.y = -1f;
        else if (mousePosition.y >= Screen.height - edgeSize) edge.y = 1f;
        return edge;
    }

    private void Smooth(float deltaTime)
    {
        pivot = Vector3.SmoothDamp(pivot, targetPivot, ref pivotVelocity, smoothTime, Mathf.Infinity, deltaTime);
        distance = Mathf.SmoothDamp(distance, targetDistance, ref distanceVelocity, smoothTime, Mathf.Infinity, deltaTime);
        currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, smoothTime, Mathf.Infinity, deltaTime);
    }

    private void ApplyPose()
    {
        Quaternion rotation = Quaternion.Euler(pitch, currentYaw, 0f);
        transform.rotation = rotation;
        transform.position = pivot - rotation * Vector3.forward * distance;
    }

    // Keeps the pivot inside the bounds (XZ) at the ground height.
    private Vector3 ClampToBounds(Vector3 point)
    {
        point.x = Mathf.Clamp(point.x, bounds.min.x, bounds.max.x);
        point.z = Mathf.Clamp(point.z, bounds.min.z, bounds.max.z);
        point.y = startPivot.y;
        return point;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }
}

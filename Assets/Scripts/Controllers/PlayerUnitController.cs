using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Player decision layer: a left click orders the unit to move to the clicked point.
/// Only talks to UnitMover. Enable or disable this component to hand the unit to another controller.
/// </summary>
public class PlayerUnitController : MonoBehaviour
{
    [SerializeField] private UnitMover mover;
    [SerializeField] private Camera viewCamera;

    private void Start()
    {
        if (mover == null || viewCamera == null)
        {
            Debug.LogError("PlayerUnitController: mover or viewCamera is not assigned.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
        {
            return;
        }

        // Cast a ray from the camera through the cursor and order the move to the hit point.
        Ray ray = viewCamera.ScreenPointToRay(mouse.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            mover.MoveTo(hit.point);
        }
    }
}

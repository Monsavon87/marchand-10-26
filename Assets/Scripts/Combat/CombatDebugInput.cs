using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Debug only: keys to test units. The damage key hurts the selected units; Shift + the key hurts the unit under
/// the mouse cursor (enemies cannot be selected). (Waiting is a button of the combat interface now.)
/// Remove or disable it once real controls exist.
/// </summary>
public class CombatDebugInput : MonoBehaviour
{
    [SerializeField] private SelectionManager selection;
    [Tooltip("Optional: falls back to the main camera.")]
    [SerializeField] private Camera viewCamera;
    [Tooltip("Keys are physical positions (US layout).")]
    [SerializeField] private Key damageKey = Key.K;
    [SerializeField] private int damage = 10;

    private void Start()
    {
        if (viewCamera == null)
        {
            viewCamera = Camera.main;
        }

        if (selection == null || viewCamera == null)
        {
            Debug.LogError("CombatDebugInput: selection or camera is not assigned.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (!keyboard[damageKey].wasPressedThisFrame)
        {
            return;
        }

        bool shift = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        if (shift)
        {
            DamageUnderCursor();
            return;
        }

        // Dead units are only destroyed at the end of the frame, so the list does not change under the loop.
        System.Collections.Generic.IReadOnlyList<SelectableUnit> selected = selection.Selected;
        for (int i = 0; i < selected.Count; i++)
        {
            Hurt(selected[i].GetComponent<Health>());
        }
    }

    private void DamageUnderCursor()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            return;
        }

        Ray ray = viewCamera.ScreenPointToRay(mouse.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Hurt(hit.collider.GetComponentInParent<Health>());
        }
    }

    private void Hurt(Health health)
    {
        if (health == null)
        {
            return;
        }

        int before = health.Current;
        health.TakeDamage(damage);
        Debug.Log("[Debug damage] " + health.name + ": " + before + " -> " + health.Current + " HP" + (health.IsDead ? " (dead)" : string.Empty));
    }
}

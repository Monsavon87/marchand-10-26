using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Owns the player's selection and interprets the left mouse button:
/// click a unit = select it (Shift toggles), drag = selection rectangle, click the ground = GroundClicked event.
/// It never gives movement orders: other scripts listen to GroundClicked.
/// </summary>
public class SelectionManager : MonoBehaviour
{
    [SerializeField] private Camera viewCamera;
    [Tooltip("Optional: the camera centers on the first selected unit when the center key is pressed.")]
    [SerializeField] private StrategyCamera strategyCamera;
    [Header("Selection rectangle")]
    [SerializeField] private Canvas selectionCanvas;
    [Tooltip("Image (bottom-left anchor and pivot) drawn while dragging the selection rectangle.")]
    [SerializeField] private RectTransform selectionBox;
    [Tooltip("Mouse movement in pixels before a click becomes a rectangle drag.")]
    [SerializeField] private float dragThreshold = 8f;

    private readonly List<SelectableUnit> selected = new List<SelectableUnit>();
    private bool isPressing;
    private bool isBoxSelecting;
    private Vector2 pressPosition;

    /// <summary>Raised whenever the set of selected units changes.</summary>
    public event Action SelectionChanged = delegate { };
    /// <summary>Raised on a simple click on the ground (not on a unit, not a drag).</summary>
    public event Action<Vector3> GroundClicked = delegate { };

    public IReadOnlyList<SelectableUnit> Selected => selected;

    private void Start()
    {
        if (viewCamera == null || selectionCanvas == null || selectionBox == null)
        {
            Debug.LogError("SelectionManager: camera, canvas or selection box is not assigned.", this);
            enabled = false;
            return;
        }

        selectionBox.gameObject.SetActive(false);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            ClearSelection();
        }

        if (mouse == null)
        {
            return;
        }

        Vector2 position = mouse.position.ReadValue();
        if (mouse.leftButton.wasPressedThisFrame)
        {
            isPressing = true;
            isBoxSelecting = false;
            pressPosition = position;
        }

        if (!isPressing)
        {
            return;
        }

        if (mouse.leftButton.isPressed)
        {
            if (!isBoxSelecting && (position - pressPosition).sqrMagnitude > dragThreshold * dragThreshold)
            {
                isBoxSelecting = true;
                selectionBox.gameObject.SetActive(true);
            }

            if (isBoxSelecting)
            {
                DrawSelectionBox(position);
            }

            return;
        }

        // Button released: the gesture was a click or a rectangle.
        isPressing = false;
        selectionBox.gameObject.SetActive(false);
        bool additive = keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
        if (isBoxSelecting)
        {
            isBoxSelecting = false;
            SelectInRectangle(position, additive);
        }
        else
        {
            HandleClick(position, additive);
        }
    }

    /// <summary>Deselects every unit.</summary>
    public void ClearSelection()
    {
        if (selected.Count == 0)
        {
            return;
        }

        for (int i = selected.Count - 1; i >= 0; i--)
        {
            Deselect(i);
        }

        NotifyChanged();
    }

    private void HandleClick(Vector2 screenPosition, bool additive)
    {
        Ray ray = viewCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            return;
        }

        SelectableUnit unit = hit.collider.GetComponentInParent<SelectableUnit>();
        if (unit == null)
        {
            GroundClicked(hit.point);
            return;
        }

        if (additive)
        {
            int index = selected.IndexOf(unit);
            if (index >= 0)
            {
                Deselect(index);
            }
            else
            {
                Select(unit);
            }
        }
        else
        {
            for (int i = selected.Count - 1; i >= 0; i--)
            {
                Deselect(i);
            }

            Select(unit);
        }

        NotifyChanged();
    }

    private void SelectInRectangle(Vector2 releasePosition, bool additive)
    {
        Rect rectangle = Rect.MinMaxRect(
            Mathf.Min(pressPosition.x, releasePosition.x), Mathf.Min(pressPosition.y, releasePosition.y),
            Mathf.Max(pressPosition.x, releasePosition.x), Mathf.Max(pressPosition.y, releasePosition.y));

        if (!additive)
        {
            for (int i = selected.Count - 1; i >= 0; i--)
            {
                Deselect(i);
            }
        }

        IReadOnlyList<SelectableUnit> all = SelectableUnit.All;
        for (int i = 0; i < all.Count; i++)
        {
            Vector3 screenPoint = viewCamera.WorldToScreenPoint(all[i].transform.position);
            if (screenPoint.z > 0f && rectangle.Contains(screenPoint) && !selected.Contains(all[i]))
            {
                Select(all[i]);
            }
        }

        NotifyChanged();
    }

    private void DrawSelectionBox(Vector2 currentPosition)
    {
        Vector2 min = Vector2.Min(pressPosition, currentPosition);
        Vector2 size = Vector2.Max(pressPosition, currentPosition) - min;
        float scale = selectionCanvas.scaleFactor;
        selectionBox.anchoredPosition = min / scale;
        selectionBox.sizeDelta = size / scale;
    }

    private void Select(SelectableUnit unit)
    {
        selected.Add(unit);
        unit.SelectionChanged += OnUnitSelectionChanged;
        unit.SetSelected(true);
    }

    private void Deselect(int index)
    {
        SelectableUnit unit = selected[index];
        selected.RemoveAt(index);
        unit.SelectionChanged -= OnUnitSelectionChanged;
        unit.SetSelected(false);
    }

    // A selected unit that gets disabled or destroyed leaves the selection by itself.
    private void OnUnitSelectionChanged(SelectableUnit unit, bool isSelected)
    {
        if (!isSelected && selected.Remove(unit))
        {
            unit.SelectionChanged -= OnUnitSelectionChanged;
            NotifyChanged();
        }
    }

    private void NotifyChanged()
    {
        if (strategyCamera != null)
        {
            strategyCamera.SetFocusTarget(selected.Count > 0 ? selected[0].transform : null);
        }

        SelectionChanged();
    }
}

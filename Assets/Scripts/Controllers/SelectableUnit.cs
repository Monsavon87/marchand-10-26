using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marks a unit as selectable by the player and shows its selection ring.
/// It knows the unit's UnitMover so command code can reach it, while UnitMover knows nothing about selection.
/// </summary>
public class SelectableUnit : MonoBehaviour
{
    private static readonly List<SelectableUnit> AllUnits = new List<SelectableUnit>();

    [SerializeField] private UnitMover mover;
    [Tooltip("Ring shown under the unit while it is selected.")]
    [SerializeField] private GameObject indicator;

    /// <summary>All enabled selectable units in the scene.</summary>
    public static IReadOnlyList<SelectableUnit> All => AllUnits;

    /// <summary>Raised when the unit is selected (true) or deselected (false).</summary>
    public event Action<SelectableUnit, bool> SelectionChanged = delegate { };

    public bool IsSelected { get; private set; }
    public UnitMover Mover => mover;

    private void Awake()
    {
        if (mover == null || indicator == null)
        {
            Debug.LogError("SelectableUnit: mover or indicator is not assigned.", this);
            enabled = false;
            return;
        }

        indicator.SetActive(false);
    }

    private void OnEnable()
    {
        AllUnits.Add(this);
    }

    private void OnDisable()
    {
        AllUnits.Remove(this);
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        if (IsSelected == selected)
        {
            return;
        }

        IsSelected = selected;
        if (indicator != null)
        {
            indicator.SetActive(selected);
        }

        SelectionChanged(this, selected);
    }
}

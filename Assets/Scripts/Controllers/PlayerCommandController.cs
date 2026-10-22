using UnityEngine;

/// <summary>
/// Turns a ground click into move orders for the selected units.
/// One unit goes exactly to the clicked point; several units spread into a small formation around it.
/// Only talks to UnitMover (through SelectableUnit); it does not read the mouse itself.
/// </summary>
public class PlayerCommandController : MonoBehaviour
{
    [SerializeField] private SelectionManager selection;
    [Tooltip("Distance between two units of the formation, in meters.")]
    [SerializeField] private float formationSpacing = 1.6f;

    // Reused buffers: orders are rare, but there is no reason to allocate each time.
    private Vector3[] slots = new Vector3[16];
    private bool[] slotTaken = new bool[16];

    private void Start()
    {
        if (selection == null)
        {
            Debug.LogError("PlayerCommandController: selection is not assigned.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (selection != null)
        {
            selection.GroundClicked += OnGroundClicked;
        }
    }

    private void OnDisable()
    {
        if (selection != null)
        {
            selection.GroundClicked -= OnGroundClicked;
        }
    }

    private void OnGroundClicked(Vector3 point)
    {
        System.Collections.Generic.IReadOnlyList<SelectableUnit> units = selection.Selected;
        int count = units.Count;
        if (count == 0)
        {
            return;
        }

        if (count == 1)
        {
            units[0].Mover.MoveTo(point);
            return;
        }

        if (slots.Length < count)
        {
            slots = new Vector3[count];
            slotTaken = new bool[count];
        }

        // The formation faces the direction of travel: from the group's center toward the clicked point.
        Vector3 centroid = Vector3.zero;
        for (int i = 0; i < count; i++)
        {
            centroid += units[i].transform.position;
        }

        centroid /= count;
        FormationLayout.Fill(point, point - centroid, count, formationSpacing, slots);

        // Each unit takes the closest free slot, so paths cross as little as possible.
        for (int i = 0; i < count; i++)
        {
            slotTaken[i] = false;
        }

        for (int unitIndex = 0; unitIndex < count; unitIndex++)
        {
            Vector3 unitPosition = units[unitIndex].transform.position;
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int s = 0; s < count; s++)
            {
                if (slotTaken[s])
                {
                    continue;
                }

                float distance = (slots[s] - unitPosition).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = s;
                }
            }

            slotTaken[best] = true;
            units[unitIndex].Mover.MoveTo(slots[best]);
        }
    }
}

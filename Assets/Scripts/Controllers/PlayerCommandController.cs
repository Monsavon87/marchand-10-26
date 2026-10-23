using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Turns a ground click into move orders for the selected units.
/// One unit goes exactly to the clicked point; several units spread into a small formation around it.
/// It never moves a unit itself: it asks the unit's ActionController (the same entry point the AI uses).
/// It does not read the mouse itself.
/// </summary>
public class PlayerCommandController : MonoBehaviour
{
    [SerializeField] private SelectionManager selection;
    [Tooltip("When on, clicking an enemy unit orders the selected units to attack it. When off, it is a plain move to that point.")]
    [SerializeField] private bool attackEnemiesOnClick;
    [Tooltip("Cancels the current order and the queue of the selected units.")]
    [SerializeField] private Key cancelKey = Key.X;
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

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard[cancelKey].wasPressedThisFrame)
        {
            return;
        }

        System.Collections.Generic.IReadOnlyList<SelectableUnit> units = selection.Selected;
        for (int i = 0; i < units.Count; i++)
        {
            ActionController actions = units[i].GetComponent<ActionController>();
            if (actions != null)
            {
                actions.Cancel();
            }
        }
    }

    // Shift held while giving an order lines it up behind the current one instead of replacing it.
    private static bool IsQueueing()
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
    }

    private void OnEnable()
    {
        if (selection != null)
        {
            selection.GroundClicked += OnGroundClicked;
            selection.UnitClicked += OnUnitClicked;
        }
    }

    private void OnDisable()
    {
        if (selection != null)
        {
            selection.GroundClicked -= OnGroundClicked;
            selection.UnitClicked -= OnUnitClicked;
        }
    }

    // A click on a unit that cannot be selected: an attack order on an enemy if the scene wants it, a plain move otherwise.
    private void OnUnitClicked(UnitTeam clicked, Vector3 point)
    {
        System.Collections.Generic.IReadOnlyList<SelectableUnit> units = selection.Selected;
        if (!attackEnemiesOnClick || units.Count == 0)
        {
            OnGroundClicked(point);
            return;
        }

        bool anyAttack = false;
        for (int i = 0; i < units.Count; i++)
        {
            UnitTeam own = units[i].GetComponent<UnitTeam>();
            if (own == null || own.Team == clicked.Team)
            {
                continue;
            }

            ActionController actions = units[i].GetComponent<ActionController>();
            if (actions == null)
            {
                Debug.LogError("PlayerCommandController: " + units[i].name + " has no ActionController.", units[i]);
                continue;
            }

            if (IsQueueing())
            {
                actions.EnqueueAttack(clicked.transform);
            }
            else
            {
                actions.RequestAttack(clicked.transform);
            }

            anyAttack = true;
        }

        if (!anyAttack)
        {
            OnGroundClicked(point);
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
            Order(units[0], point);
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
            Order(units[unitIndex], slots[best]);
        }
    }

    // The action controller validates the request, may refuse it (and says why in the console), and runs it.
    private void Order(SelectableUnit unit, Vector3 destination)
    {
        ActionController actions = unit.GetComponent<ActionController>();
        if (actions == null)
        {
            Debug.LogError("PlayerCommandController: " + unit.name + " has no ActionController.", unit);
            return;
        }

        if (IsQueueing())
        {
            actions.EnqueueMove(destination);
        }
        else
        {
            actions.RequestMove(destination);
        }
    }
}

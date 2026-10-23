using UnityEngine;

/// <summary>
/// Draws on the ground, around the selected unit, how far its weapon reaches: an outer circle (the reach of a
/// melee weapon, the maximum range of a ranged one) and, when the weapon has a dead zone, an inner circle inside
/// which it cannot strike. Read-only: it only reads the unit's stats.
/// </summary>
public class WeaponRangeDisplay : MonoBehaviour
{
    private const int Segments = 64;

    [SerializeField] private SelectionManager selection;
    [Tooltip("Circle for the weapon's reach / maximum range. Its points are local to this object.")]
    [SerializeField] private LineRenderer reachCircle;
    [Tooltip("Circle for the weapon's dead zone. Hidden when the weapon has none.")]
    [SerializeField] private LineRenderer deadZoneCircle;
    [Tooltip("Height of the circles above the ground, in meters.")]
    [SerializeField] private float height = 0.07f;

    private readonly Vector3[] points = new Vector3[Segments];
    private SelectableUnit followed;

    private void Start()
    {
        if (selection == null || reachCircle == null || deadZoneCircle == null)
        {
            Debug.LogError("WeaponRangeDisplay: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        reachCircle.loop = true;
        reachCircle.useWorldSpace = false;
        deadZoneCircle.loop = true;
        deadZoneCircle.useWorldSpace = false;
        Refresh();
    }

    private void OnEnable()
    {
        if (selection != null)
        {
            selection.SelectionChanged += Refresh;
        }
    }

    private void OnDisable()
    {
        if (selection != null)
        {
            selection.SelectionChanged -= Refresh;
        }
    }

    // The circles follow the unit. A unit that was destroyed leaves the selection, which refreshes this.
    private void LateUpdate()
    {
        if (followed != null)
        {
            transform.position = followed.Mover.GroundPosition + Vector3.up * height;
        }
    }

    private void Refresh()
    {
        followed = selection.Selected.Count > 0 ? selection.Selected[0] : null;
        UnitStats stats = followed != null ? followed.GetComponent<UnitStats>() : null;
        if (stats == null)
        {
            followed = null;
            reachCircle.enabled = false;
            deadZoneCircle.enabled = false;
            return;
        }

        float outer = stats.IsRanged ? stats.MaxRange : stats.Reach;
        Draw(reachCircle, outer);
        Draw(deadZoneCircle, stats.DeadZone);
        LateUpdate();
    }

    private void Draw(LineRenderer line, float radius)
    {
        if (radius <= 0.01f)
        {
            line.enabled = false;
            return;
        }

        for (int i = 0; i < Segments; i++)
        {
            float angle = i * Mathf.PI * 2f / Segments;
            points[i] = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        line.positionCount = Segments;
        line.SetPositions(points);
        line.enabled = true;
    }
}

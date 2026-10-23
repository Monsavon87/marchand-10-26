using UnityEngine;

/// <summary>
/// Line-of-sight test shared by everything that needs to know whether something can be seen: a ray from one point
/// to another is blocked by any solid object on the given layers, except units (they never block the view).
/// </summary>
public static class LineOfSight
{
    private const int MaxHits = 16;
    private static readonly RaycastHit[] Hits = new RaycastHit[MaxHits];

    /// <summary>True if nothing solid (other than a unit) lies between the two points.</summary>
    public static bool IsClear(Vector3 from, Vector3 to, LayerMask blockers)
    {
        Vector3 offset = to - from;
        float distance = offset.magnitude;
        if (distance < 0.01f)
        {
            return true;
        }

        int count = Physics.RaycastNonAlloc(from, offset / distance, Hits, distance, blockers, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            if (Hits[i].collider.GetComponentInParent<UnitTeam>() == null)
            {
                return false;
            }
        }

        return true;
    }
}

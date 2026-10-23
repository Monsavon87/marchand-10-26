using UnityEngine;

/// <summary>
/// Line-of-sight test shared by everything that needs to know whether something can be seen or shot: a ray from one
/// point to another is blocked by any solid object on the given layers, except units (they never block the view)
/// and scenery marked ShotPassThrough.
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
            if (IsSolid(Hits[i].collider))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// The same test for a shot. Returns true when no solid object is in the way. 'interceptor' is the first unit
    /// the arrow would meet before the target (null when there is none, or when a wall comes first): any unit
    /// other than the shooter and the target, but a unit of the shooter's own side only when friendly fire is on.
    /// </summary>
    public static bool TraceShot(Vector3 from, Vector3 to, LayerMask blockers, Transform shooter, Transform target,
        Team shooterTeam, bool friendlyFire, out UnitTeam interceptor)
    {
        interceptor = null;
        Vector3 offset = to - from;
        float distance = offset.magnitude;
        if (distance < 0.01f)
        {
            return true;
        }

        int count = Physics.RaycastNonAlloc(from, offset / distance, Hits, distance, blockers, QueryTriggerInteraction.Ignore);
        float nearestSolid = float.MaxValue;
        float nearestUnit = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            Collider collider = Hits[i].collider;
            UnitTeam unit = collider.GetComponentInParent<UnitTeam>();
            if (unit == null)
            {
                if (IsSolid(collider) && Hits[i].distance < nearestSolid)
                {
                    nearestSolid = Hits[i].distance;
                }

                continue;
            }

            if (unit.transform == shooter || unit.transform == target || (!friendlyFire && unit.Team == shooterTeam))
            {
                continue;
            }

            Health health = unit.GetComponent<Health>();
            if ((health == null || !health.IsDead) && Hits[i].distance < nearestUnit)
            {
                nearestUnit = Hits[i].distance;
                interceptor = unit;
            }
        }

        if (nearestUnit > nearestSolid)
        {
            interceptor = null;
        }

        return nearestSolid == float.MaxValue;
    }

    // A unit never blocks, and neither does scenery marked as pass-through.
    private static bool IsSolid(Collider collider)
    {
        return collider.GetComponentInParent<UnitTeam>() == null && collider.GetComponentInParent<ShotPassThrough>() == null;
    }
}

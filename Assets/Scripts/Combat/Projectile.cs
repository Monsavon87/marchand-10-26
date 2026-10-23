using System;
using UnityEngine;

/// <summary>
/// An arrow in flight. It flies in a straight line to the point it was aimed at (it does not follow its target) and
/// reports where it lands: the unit it hit on the way, or nobody if it reached the point without meeting one.
/// It hits the first unit it meets, except its shooter and, unless friendly fire is on, the shooter's own side.
/// It moves with game time, so it freezes while the game is paused. It never decides what a hit does: the one who
/// shot it resolves the attack when told where it landed.
/// </summary>
public class Projectile : MonoBehaviour
{
    private const int MaxHits = 8;
    private static readonly RaycastHit[] Hits = new RaycastHit[MaxHits];

    [Tooltip("Seconds an arrow that hit nobody stays stuck where it landed.")]
    [Min(0f)] [SerializeField] private float lingerTime = 1.5f;

    private Vector3 destination;
    private float speed;
    private float radius;
    private Transform shooter;
    private Team shooterTeam;
    private bool friendlyFire;
    private Action<UnitTeam, Vector3> landed = delegate { };
    private bool flying;
    private float lingerLeft;

    /// <summary>
    /// Sends the arrow from where it is toward 'aim'. 'onLanded' is called once, with the unit that was hit (null if
    /// none) and the point of impact.
    /// </summary>
    public void Launch(Vector3 aim, float flightSpeed, float hitRadius, Transform shooterTransform, Team side,
        bool hitsOwnSide, Action<UnitTeam, Vector3> onLanded)
    {
        destination = aim;
        speed = flightSpeed;
        radius = hitRadius;
        shooter = shooterTransform;
        shooterTeam = side;
        friendlyFire = hitsOwnSide;
        landed = onLanded ?? delegate { };
        flying = true;

        Vector3 direction = aim - transform.position;
        if (direction.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void Update()
    {
        if (!flying)
        {
            lingerLeft -= Time.deltaTime;
            if (lingerLeft <= 0f)
            {
                Destroy(gameObject);
            }

            return;
        }

        Vector3 position = transform.position;
        Vector3 toDestination = destination - position;
        float step = speed * Time.deltaTime;
        bool arrives = toDestination.magnitude <= step;
        Vector3 movement = arrives ? toDestination : toDestination.normalized * step;
        float length = movement.magnitude;

        if (length > 0.0001f)
        {
            Vector3 direction = movement / length;
            transform.rotation = Quaternion.LookRotation(direction);

            UnitTeam victim = FindVictim(position, direction, length, out float hitDistance);
            if (victim != null)
            {
                transform.position = position + direction * hitDistance;
                Land(victim);
                return;
            }
        }

        transform.position = position + movement;
        if (arrives)
        {
            Land(null);
        }
    }

    // The nearest unit this arrow can hit along the part of its flight it covers this frame.
    private UnitTeam FindVictim(Vector3 from, Vector3 direction, float length, out float distance)
    {
        distance = 0f;
        int count = Physics.SphereCastNonAlloc(from, radius, direction, Hits, length, ~0, QueryTriggerInteraction.Ignore);
        UnitTeam victim = null;
        float nearest = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            UnitTeam unit = Hits[i].collider.GetComponentInParent<UnitTeam>();
            if (unit == null || unit.transform == shooter || (!friendlyFire && unit.Team == shooterTeam))
            {
                continue;
            }

            Health health = unit.GetComponent<Health>();
            if (health != null && health.IsDead)
            {
                continue;
            }

            if (Hits[i].distance < nearest)
            {
                nearest = Hits[i].distance;
                victim = unit;
            }
        }

        distance = nearest == float.MaxValue ? 0f : nearest;
        return victim;
    }

    private void Land(UnitTeam victim)
    {
        flying = false;
        Action<UnitTeam, Vector3> callback = landed;
        landed = delegate { };
        callback(victim, transform.position);

        if (victim != null)
        {
            Destroy(gameObject);
        }
        else
        {
            lingerLeft = lingerTime;
        }
    }
}

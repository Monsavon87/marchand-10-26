using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Looks around a target for a good place to shoot from: reachable on the NavMesh, within the weapon's range, not
/// too close, with a clear line to the target. Used by the shoot action (to find another spot when its shot is
/// blocked) and by the AI (to keep its distance). The best spot is the one that is the shortest walk away, plus an
/// optional penalty for spots the caller finds risky. Searching costs a few ray and path queries, so it is only
/// done when a decision is needed, never every frame.
/// </summary>
public static class ShotSpotFinder
{
    // Rings around the target (fractions of the range) and angles tried.
    private static readonly float[] Rings = { 0.5f, 0.7f, 0.85f };
    private const int Angles = 12;
    private const float SnapRadius = 1.5f;

    /// <summary>
    /// Finds the best spot. 'minDistance' is the closest the unit accepts to be from the target (at least the dead
    /// zone). 'exposure' (optional) gives a risk value for a position, counted as 'exposureWeight' meters of walk
    /// per unit of risk.
    /// </summary>
    public static bool TryFind(UnitStats stats, UnitMover mover, CombatSettings settings, Team team,
        Vector3 fromPosition, Transform target, float minDistance, Func<Vector3, float> exposure, float exposureWeight,
        out Vector3 spot)
    {
        spot = default;
        Vector3 targetPosition = target.position;
        float nearLimit = Mathf.Max(stats.DeadZone * 1.05f, minDistance);
        float farLimit = stats.MaxRange * 0.98f;
        if (nearLimit >= farLimit)
        {
            return false;
        }

        Vector3 away = fromPosition - targetPosition;
        away.y = 0f;
        float startAngle = away.sqrMagnitude > 0.0001f ? Mathf.Atan2(away.z, away.x) : 0f;
        Vector3 eye = Vector3.up * settings.EyeHeight;

        bool found = false;
        float bestScore = float.MaxValue;
        for (int ring = 0; ring < Rings.Length; ring++)
        {
            float radius = Mathf.Clamp(stats.MaxRange * Rings[ring], nearLimit * 1.1f, farLimit * 0.97f);
            for (int i = 0; i < Angles; i++)
            {
                float angle = startAngle + i * Mathf.PI * 2f / Angles;
                Vector3 candidate = targetPosition + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, SnapRadius, NavMesh.AllAreas))
                {
                    continue;
                }

                float dx = hit.position.x - targetPosition.x;
                float dz = hit.position.z - targetPosition.z;
                float spotDistance = Mathf.Sqrt(dx * dx + dz * dz);
                if (spotDistance < nearLimit || spotDistance > farLimit
                    || !LineOfSight.TraceShot(hit.position + eye, targetPosition + eye, settings.SightBlockers,
                        mover.transform, target, team, settings.AllowFriendlyFire, out _)
                    || !mover.TryGetPath(hit.position, out float length, out _))
                {
                    continue;
                }

                float score = length + (exposure != null ? exposure(hit.position) * exposureWeight : 0f);
                if (score < bestScore)
                {
                    found = true;
                    bestScore = score;
                    spot = hit.position;
                }
            }
        }

        return found;
    }
}

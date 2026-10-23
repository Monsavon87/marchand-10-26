using UnityEngine;

/// <summary>
/// Draws, on the ground around a unit, its four facing arcs: front, back, and the two flanks, oriented like the
/// unit. The arc the attacker stands in is drawn thicker. Primitives only (line renderers made once at startup).
/// It only draws what it is told to; the zone itself is worked out by the combat rules.
/// </summary>
public class FacingZoneIndicator : MonoBehaviour
{
    private const int ArcCount = 4;
    private const int FrontArc = 0;
    private const int RightFlankArc = 1;
    private const int BackArc = 2;
    private const int LeftFlankArc = 3;
    private const int SegmentsPerArc = 16;

    [SerializeField] private Material frontMaterial;
    [SerializeField] private Material flankMaterial;
    [SerializeField] private Material backMaterial;
    [Tooltip("Radius of the arcs around the unit, in meters.")]
    [SerializeField] private float radius = 1.4f;
    [Tooltip("Height of the arcs above the ground, in meters.")]
    [SerializeField] private float height = 0.09f;
    [SerializeField] private float normalWidth = 0.05f;
    [Tooltip("Width of the arc of the zone that would be hit.")]
    [SerializeField] private float highlightWidth = 0.2f;

    private readonly LineRenderer[] arcs = new LineRenderer[ArcCount];
    private readonly Vector3[] points = new Vector3[SegmentsPerArc + 1];

    private void Awake()
    {
        if (frontMaterial == null || flankMaterial == null || backMaterial == null)
        {
            Debug.LogError("FacingZoneIndicator: a material is not assigned.", this);
            enabled = false;
            return;
        }

        Material[] materials = { frontMaterial, flankMaterial, flankMaterial, backMaterial };
        for (int i = 0; i < ArcCount; i++)
        {
            var go = new GameObject("ZoneArc_" + i);
            go.transform.SetParent(transform, false);
            LineRenderer line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.loop = false;
            line.alignment = LineAlignment.TransformZ;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.sharedMaterial = materials[i];
            line.positionCount = 0;
            line.enabled = false;
            arcs[i] = line;
        }
    }

    /// <summary>
    /// Shows the zones around a unit standing at 'ground' and looking toward 'forward'. 'toAttacker' tells on which
    /// side the attacker is (to pick the flank), 'hitZone' which zone is highlighted.
    /// </summary>
    public void Show(Vector3 ground, Vector3 forward, Vector3 toAttacker, AttackZone hitZone, AttackZoneSettings zones)
    {
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
        {
            Hide();
            return;
        }

        forward.Normalize();
        float front = zones.FrontHalfAngle;
        float back = zones.BackHalfAngle;
        Vector3 center = ground + Vector3.up * height;

        // Angles are measured from the unit's forward, positive toward its right.
        toAttacker.y = 0f;
        bool attackerOnRight = Vector3.SignedAngle(forward, toAttacker, Vector3.up) >= 0f;

        DrawArc(arcs[FrontArc], center, forward, -front, front, hitZone == AttackZone.Front);
        DrawArc(arcs[RightFlankArc], center, forward, front, 180f - back, hitZone == AttackZone.Flank && attackerOnRight);
        DrawArc(arcs[BackArc], center, forward, 180f - back, 180f + back, hitZone == AttackZone.Back);
        DrawArc(arcs[LeftFlankArc], center, forward, 180f + back, 360f - front, hitZone == AttackZone.Flank && !attackerOnRight);
    }

    public void Hide()
    {
        for (int i = 0; i < ArcCount; i++)
        {
            if (arcs[i] != null)
            {
                arcs[i].enabled = false;
            }
        }
    }

    private void DrawArc(LineRenderer line, Vector3 center, Vector3 forward, float fromAngle, float toAngle, bool highlighted)
    {
        for (int i = 0; i <= SegmentsPerArc; i++)
        {
            float angle = Mathf.Lerp(fromAngle, toAngle, i / (float)SegmentsPerArc);
            points[i] = center + Quaternion.Euler(0f, angle, 0f) * forward * radius;
        }

        float width = highlighted ? highlightWidth : normalWidth;
        line.startWidth = width;
        line.endWidth = width;
        line.positionCount = SegmentsPerArc + 1;
        line.SetPositions(points);
        line.enabled = true;
    }
}

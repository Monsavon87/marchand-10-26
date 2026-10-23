using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// While a unit is selected, shows what the player would attack: the enemy under the mouse cursor gets a red ring
/// on the ground, the zones around it (front, flank, back) with the one that would be hit, and a label with the
/// chance to hit and what that zone is worth. Read-only: the click itself is handled by the player's command
/// controller.
/// For a ranged unit it also draws the line of fire to the enemy (green when clear, red when a wall or pillar is in
/// the way or the enemy is too close) and adds the distance, its hit penalty and any problem to the label.
/// </summary>
public class CombatTargeting : MonoBehaviour
{
    [SerializeField] private SelectionManager selection;
    [SerializeField] private CombatRulesProvider rulesProvider;
    [Tooltip("Gives the eye height, the layers that block a shot and the friendly fire setting.")]
    [SerializeField] private CombatManager combat;
    [Tooltip("Optional: falls back to the main camera.")]
    [SerializeField] private Camera viewCamera;
    [Tooltip("Flat ring shown under the targeted enemy. It must have no collider, or it would block clicks.")]
    [SerializeField] private Transform highlightRing;
    [Tooltip("World text above the targeted enemy, for the chance to hit.")]
    [SerializeField] private TextMesh chanceLabel;
    [Tooltip("Draws the front / flank / back zones around the targeted enemy.")]
    [SerializeField] private FacingZoneIndicator zoneIndicator;
    [Tooltip("Line of fire from a ranged unit to the enemy under the cursor. It must have no collider.")]
    [SerializeField] private LineRenderer shotLine;
    [SerializeField] private Material clearShotMaterial;
    [SerializeField] private Material blockedShotMaterial;
    [SerializeField] private float labelHeight = 2.7f;
    [SerializeField] private float ringHeight = 0.08f;

    private UnitTeam attackerTeam;
    private UnitStats attackerStats;
    private UnitTeam hoveredTeam;
    private UnitStats hoveredStats;
    private UnitMover hoveredMover;

    private void Start()
    {
        if (viewCamera == null)
        {
            viewCamera = Camera.main;
        }

        if (selection == null || rulesProvider == null || combat == null || viewCamera == null || highlightRing == null
            || chanceLabel == null || zoneIndicator == null || shotLine == null || clearShotMaterial == null
            || blockedShotMaterial == null)
        {
            Debug.LogError("CombatTargeting: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        chanceLabel.font = font;
        chanceLabel.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        Hide();
    }

    private void OnEnable()
    {
        if (selection != null)
        {
            selection.SelectionChanged += OnSelectionChanged;
        }
    }

    private void OnDisable()
    {
        if (selection != null)
        {
            selection.SelectionChanged -= OnSelectionChanged;
        }
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || attackerStats == null || rulesProvider.Rules == null)
        {
            Hide();
            return;
        }

        Ray ray = viewCamera.ScreenPointToRay(mouse.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            Hide();
            return;
        }

        UnitTeam team = hit.collider.GetComponentInParent<UnitTeam>();
        if (team == null || team.Team == attackerTeam.Team)
        {
            Hide();
            return;
        }

        if (team != hoveredTeam)
        {
            hoveredTeam = team;
            hoveredStats = team.GetComponent<UnitStats>();
            hoveredMover = team.GetComponent<UnitMover>();
        }

        if (hoveredStats == null || hoveredMover == null)
        {
            Hide();
            return;
        }

        Vector3 ground = hoveredMover.GroundPosition;
        highlightRing.position = ground + Vector3.up * ringHeight;
        highlightRing.gameObject.SetActive(true);

        // The attacker would arrive in a straight line from where it stands: that is the zone it would hit.
        ICombatRules rules = rulesProvider.Rules;
        Vector3 attackerPosition = attackerStats.transform.position;
        AttackZone zone = rules.DetermineZone(attackerPosition, team.transform);
        ZoneModifier modifier = rules.GetZoneModifier(zone);
        int rangeModifier = 0;
        string shotInfo = string.Empty;
        bool shotBlocked = false;
        if (attackerStats.IsRanged)
        {
            rangeModifier = PreviewShot(rules, team, ground, out shotInfo, out shotBlocked);
        }
        else
        {
            shotLine.enabled = false;
        }

        chanceLabel.text = shotInfo + "Toucher : " + rules.HitChancePercent(attackerStats, hoveredStats, zone, rangeModifier) + " %\n"
            + char.ToUpper(AttackZones.Name(zone)[0]) + AttackZones.Name(zone).Substring(1)
            + (modifier.hitBonus == 0 && modifier.damageBonus == 0
                ? " : aucun bonus"
                : " : +" + modifier.hitBonus + " toucher, +" + modifier.damageBonus + " dégâts");
        chanceLabel.color = shotBlocked ? new Color(1f, 0.35f, 0.3f) : Color.white;
        zoneIndicator.Show(ground, team.transform.forward, attackerPosition - team.transform.position, zone,
            rulesProvider.ZoneSettings);
        chanceLabel.transform.position = ground + Vector3.up * labelHeight;
        chanceLabel.transform.rotation = viewCamera.transform.rotation;
        chanceLabel.gameObject.SetActive(true);
    }

    // Draws the line of fire and returns the distance modifier. 'info' holds the lines about distance and problems.
    private int PreviewShot(ICombatRules rules, UnitTeam target, Vector3 targetGround, out string info, out bool blocked)
    {
        CombatSettings settings = combat.Settings;
        Vector3 shooterPosition = attackerStats.transform.position;
        Vector3 eye = Vector3.up * settings.EyeHeight;
        float dx = shooterPosition.x - targetGround.x;
        float dz = shooterPosition.z - targetGround.z;
        float distance = Mathf.Sqrt(dx * dx + dz * dz);
        int modifier = rules.GetRangeModifier(distance, attackerStats.MaxRange);

        bool tooClose = distance < attackerStats.DeadZone;
        bool outOfRange = distance > attackerStats.MaxRange;
        bool clear = LineOfSight.TraceShot(shooterPosition + eye, target.transform.position + eye, settings.SightBlockers,
            attackerStats.transform, target.transform, attackerTeam.Team, settings.AllowFriendlyFire,
            out UnitTeam interceptor);
        blocked = tooClose || !clear;

        info = "Distance : " + distance.ToString("F1") + " m ("
            + (modifier == 0 ? "aucun malus" : modifier + " toucher") + ")\n";
        if (tooClose)
        {
            info += "TROP PRÈS (zone morte)\n";
        }
        else if (!clear)
        {
            info += "TIR BLOQUÉ\n";
        }
        else if (interceptor != null)
        {
            info += "Touchera " + interceptor.name + " en chemin\n";
        }

        if (outOfRange && !blocked)
        {
            info += "Hors de portée : l'unité s'approchera\n";
        }

        shotLine.sharedMaterial = blocked ? blockedShotMaterial : clearShotMaterial;
        shotLine.positionCount = 2;
        shotLine.SetPosition(0, shooterPosition + eye);
        shotLine.SetPosition(1, target.transform.position + eye);
        shotLine.enabled = true;
        return modifier;
    }

    // The first selected unit is the attacker the preview is computed for.
    private void OnSelectionChanged()
    {
        attackerTeam = null;
        attackerStats = null;
        if (selection.Selected.Count > 0)
        {
            attackerTeam = selection.Selected[0].GetComponent<UnitTeam>();
            attackerStats = selection.Selected[0].GetComponent<UnitStats>();
            if (attackerTeam == null)
            {
                attackerStats = null;
            }
        }

        Hide();
    }

    private void Hide()
    {
        hoveredTeam = null;
        highlightRing.gameObject.SetActive(false);
        chanceLabel.gameObject.SetActive(false);
        zoneIndicator.Hide();
        shotLine.enabled = false;
    }
}

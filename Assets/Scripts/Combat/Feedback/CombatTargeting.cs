using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// While a unit is selected, shows what the player would attack: the enemy under the mouse cursor gets a red ring
/// on the ground, the zones around it (front, flank, back) with the one that would be hit, and a label with the
/// chance to hit and what that zone is worth. Read-only: the click itself is handled by the player's command
/// controller.
/// </summary>
public class CombatTargeting : MonoBehaviour
{
    [SerializeField] private SelectionManager selection;
    [SerializeField] private CombatRulesProvider rulesProvider;
    [Tooltip("Optional: falls back to the main camera.")]
    [SerializeField] private Camera viewCamera;
    [Tooltip("Flat ring shown under the targeted enemy. It must have no collider, or it would block clicks.")]
    [SerializeField] private Transform highlightRing;
    [Tooltip("World text above the targeted enemy, for the chance to hit.")]
    [SerializeField] private TextMesh chanceLabel;
    [Tooltip("Draws the front / flank / back zones around the targeted enemy.")]
    [SerializeField] private FacingZoneIndicator zoneIndicator;
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

        if (selection == null || rulesProvider == null || viewCamera == null || highlightRing == null || chanceLabel == null
            || zoneIndicator == null)
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
        chanceLabel.text = "Toucher : " + rules.HitChancePercent(attackerStats, hoveredStats, zone) + " %\n"
            + char.ToUpper(AttackZones.Name(zone)[0]) + AttackZones.Name(zone).Substring(1)
            + (modifier.hitBonus == 0 && modifier.damageBonus == 0
                ? " : aucun bonus"
                : " : +" + modifier.hitBonus + " toucher, +" + modifier.damageBonus + " dégâts");
        zoneIndicator.Show(ground, team.transform.forward, attackerPosition - team.transform.position, zone,
            rulesProvider.ZoneSettings);
        chanceLabel.transform.position = ground + Vector3.up * labelHeight;
        chanceLabel.transform.rotation = viewCamera.transform.rotation;
        chanceLabel.gameObject.SetActive(true);
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
    }
}

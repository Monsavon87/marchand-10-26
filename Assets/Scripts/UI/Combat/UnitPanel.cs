using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The panel of the selected unit: name, hit points, armor, weapon and what it is doing. It only reads the unit's
/// stats, health and orders; it shows the first selected unit (and how many others are selected with it).
/// Refreshes a few times a second on real time, so it stays readable while the game is paused.
/// </summary>
public class UnitPanel : MonoBehaviour
{
    [SerializeField] private SelectionManager selection;
    [Tooltip("The whole panel: hidden while nothing is selected.")]
    [SerializeField] private GameObject root;
    [SerializeField] private Text titleText;
    [SerializeField] private Text healthText;
    [Tooltip("Bar that shrinks from the right as hit points are lost (stretched inside its background, anchors set in the Inspector).")]
    [SerializeField] private RectTransform healthFill;
    [SerializeField] private Text detailsText;
    [SerializeField] private Text ordersText;
    [Min(0.02f)] [SerializeField] private float refreshInterval = 0.1f;

    private float nextRefresh;

    private void Start()
    {
        if (selection == null || root == null || titleText == null || healthText == null || healthFill == null
            || detailsText == null || ordersText == null)
        {
            Debug.LogError("UnitPanel: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

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

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh)
        {
            return;
        }

        nextRefresh = Time.unscaledTime + refreshInterval;
        Refresh();
    }

    private void Refresh()
    {
        if (selection.Selected.Count == 0)
        {
            root.SetActive(false);
            return;
        }

        SelectableUnit unit = selection.Selected[0];
        UnitStats stats = unit.GetComponent<UnitStats>();
        Health health = unit.GetComponent<Health>();
        if (stats == null || health == null)
        {
            root.SetActive(false);
            return;
        }

        root.SetActive(true);
        int others = selection.Selected.Count - 1;
        titleText.text = unit.name + " (" + stats.DisplayName + ")" + (others > 0 ? "  +" + others : string.Empty);

        healthText.text = "PV " + health.Current + " / " + health.Max;
        healthFill.anchorMax = new Vector2(health.Max > 0 ? Mathf.Clamp01((float)health.Current / health.Max) : 0f, 1f);

        string weapon = stats.IsRanged
            ? "Arme : à distance, " + stats.WeaponDamage + " dégâts, portée " + stats.MaxRange.ToString("0.#") + " m"
                + (stats.DeadZone > 0f ? " (zone morte " + stats.DeadZone.ToString("0.#") + " m)" : string.Empty)
                + ", " + stats.AttackTime.ToString("0.#") + " s par tir"
            : "Arme : mêlée, " + stats.WeaponDamage + " dégâts, allonge " + stats.Reach.ToString("0.#") + " m, "
                + stats.AttackTime.ToString("0.#") + " s par coup";
        detailsText.text = "Armure " + stats.Armor + "   Défense " + stats.Defense + "   Attaque +" + stats.AttackBonus + "\n" + weapon;

        ActionController actions = unit.GetComponent<ActionController>();
        ordersText.text = actions != null ? "Ordres : " + actions.DescribeOrders() : string.Empty;
    }
}

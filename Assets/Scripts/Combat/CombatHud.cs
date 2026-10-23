using UnityEngine;

/// <summary>
/// Combat readout: the PAUSE banner (and why the game paused by itself), the end-of-combat message, and the orders
/// of the first selected unit (what it does now and what is lined up behind). Read-only.
/// Uses the built-in legacy Text (no font import needed).
/// </summary>
public class CombatHud : MonoBehaviour
{
    [SerializeField] private CombatManager combat;
    [Tooltip("Shown while the game is paused (banner and screen tint).")]
    [SerializeField] private GameObject pauseGroup;
    [SerializeField] private SelectionManager selection;
    [Tooltip("Message line: automatic pause reason, end of combat.")]
    [SerializeField] private UnityEngine.UI.Text bannerLabel;
    [Tooltip("The orders of the first selected unit.")]
    [SerializeField] private UnityEngine.UI.Text ordersLabel;
    [Tooltip("Seconds between two refreshes of the orders text.")]
    [SerializeField] private float refreshInterval = 0.1f;

    private float nextRefresh;

    private void Start()
    {
        if (combat == null || pauseGroup == null || selection == null || bannerLabel == null || ordersLabel == null)
        {
            Debug.LogError("CombatHud: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        pauseGroup.SetActive(combat.IsPaused);
        bannerLabel.text = string.Empty;
        ordersLabel.text = string.Empty;
    }

    private void OnEnable()
    {
        if (combat != null)
        {
            combat.PauseChanged += OnPauseChanged;
            combat.CombatEnded += OnCombatEnded;
        }
    }

    private void OnDisable()
    {
        if (combat != null)
        {
            combat.PauseChanged -= OnPauseChanged;
            combat.CombatEnded -= OnCombatEnded;
        }
    }

    private void Update()
    {
        // Unscaled time: the orders stay readable while the game is paused.
        if (Time.unscaledTime < nextRefresh)
        {
            return;
        }

        nextRefresh = Time.unscaledTime + refreshInterval;
        string orders = string.Empty;
        if (selection.Selected.Count > 0)
        {
            ActionController actions = selection.Selected[0].GetComponent<ActionController>();
            if (actions != null)
            {
                orders = selection.Selected[0].name + " : " + actions.DescribeOrders();
            }
        }

        ordersLabel.text = orders;
    }

    private void OnPauseChanged(bool paused)
    {
        pauseGroup.SetActive(paused);
        if (combat.State == CombatState.Ended)
        {
            return;
        }

        switch (paused ? combat.PauseCause : PauseCause.None)
        {
            case PauseCause.AllyDied:
                bannerLabel.text = "Pause automatique : un allié est tombé";
                break;
            case PauseCause.EnemySighted:
                bannerLabel.text = "Pause automatique : un ennemi est en vue";
                break;
            default:
                bannerLabel.text = string.Empty;
                break;
        }
    }

    private void OnCombatEnded(Team? winner)
    {
        bannerLabel.text = winner.HasValue
            ? (winner.Value == Team.Player ? "Victoire !" : "Défaite...")
            : "Match nul";
    }
}

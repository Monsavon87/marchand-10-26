using UnityEngine;

/// <summary>
/// Minimal combat readout: PAUSE banner, round timer, and the action time left for the first selected unit.
/// Uses the built-in legacy Text (no font import needed). Read-only.
/// </summary>
public class CombatHud : MonoBehaviour
{
    [SerializeField] private CombatManager combat;
    [SerializeField] private SelectionManager selection;
    [SerializeField] private UnityEngine.UI.Text roundLabel;
    [SerializeField] private UnityEngine.UI.Text budgetLabel;
    [Tooltip("Shown while the game is paused (banner and screen tint).")]
    [SerializeField] private GameObject pauseGroup;
    [Tooltip("Seconds between two refreshes of the texts.")]
    [SerializeField] private float refreshInterval = 0.1f;

    private float nextRefresh;

    private void Start()
    {
        if (combat == null || selection == null || roundLabel == null || budgetLabel == null || pauseGroup == null)
        {
            Debug.LogError("CombatHud: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        pauseGroup.SetActive(combat.IsPaused);
    }

    private void OnEnable()
    {
        if (combat != null)
        {
            combat.PauseChanged += OnPauseChanged;
        }
    }

    private void OnDisable()
    {
        if (combat != null)
        {
            combat.PauseChanged -= OnPauseChanged;
        }
    }

    private void Update()
    {
        // Unscaled time: the HUD keeps updating while the game is paused.
        if (Time.unscaledTime < nextRefresh)
        {
            return;
        }

        nextRefresh = Time.unscaledTime + refreshInterval;
        roundLabel.text = "Manche " + combat.RoundNumber + "  -  " + combat.RoundTimeRemaining.ToString("F1") + " s";

        string budgetText = string.Empty;
        if (selection.Selected.Count > 0)
        {
            CombatMovementBudget budget = combat.FindBudget(selection.Selected[0].Mover);
            if (budget != null)
            {
                budgetText = "Temps : " + budget.Remaining.ToString("F1") + " / " + budget.Max.ToString("F1") + " s";
            }
        }

        budgetLabel.text = budgetText;
    }

    private void OnPauseChanged(bool paused)
    {
        pauseGroup.SetActive(paused);
    }
}

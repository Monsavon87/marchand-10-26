using UnityEngine;

/// <summary>
/// Combat readout: the PAUSE banner (and why the game paused by itself) and the end-of-combat message. Read-only.
/// The unit panel, the orders and the buttons are separate scripts of the combat interface.
/// Uses the built-in legacy Text (no font import needed).
/// </summary>
public class CombatHud : MonoBehaviour
{
    [SerializeField] private CombatManager combat;
    [Tooltip("Shown while the game is paused (banner and screen tint).")]
    [SerializeField] private GameObject pauseGroup;
    [Tooltip("Message line: automatic pause reason, end of combat.")]
    [SerializeField] private UnityEngine.UI.Text bannerLabel;

    private void Start()
    {
        if (combat == null || pauseGroup == null || bannerLabel == null)
        {
            Debug.LogError("CombatHud: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        pauseGroup.SetActive(combat.IsPaused);
        bannerLabel.text = string.Empty;
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

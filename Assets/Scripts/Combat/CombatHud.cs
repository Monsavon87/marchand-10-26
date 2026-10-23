using UnityEngine;

/// <summary>
/// Minimal combat readout: the PAUSE banner. Read-only.
/// </summary>
public class CombatHud : MonoBehaviour
{
    [SerializeField] private CombatManager combat;
    [Tooltip("Shown while the game is paused (banner and screen tint).")]
    [SerializeField] private GameObject pauseGroup;

    private void Start()
    {
        if (combat == null || pauseGroup == null)
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

    private void OnPauseChanged(bool paused)
    {
        pauseGroup.SetActive(paused);
    }
}

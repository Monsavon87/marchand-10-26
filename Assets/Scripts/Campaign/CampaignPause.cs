using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Pause of the campaign map: the pause key freezes everything that moves (caravan, patrols) and shows a banner;
/// pressed again, the map goes on. A window that needs the map frozen while it is open (a town) holds the pause
/// through SetHeld, independently of the player's own pause.
/// </summary>
public class CampaignPause : MonoBehaviour
{
    [Tooltip("Keys are physical positions (US layout).")]
    [SerializeField] private Key pauseKey = Key.Space;
    [Tooltip("Shown while the player has paused the map.")]
    [SerializeField] private GameObject banner;

    private bool pausedByPlayer;
    private bool held;

    public bool IsPaused => pausedByPlayer || held;

    private void Start()
    {
        if (banner == null)
        {
            Debug.LogError("CampaignPause: banner is not assigned.", this);
            enabled = false;
            return;
        }

        Apply();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard[pauseKey].wasPressedThisFrame)
        {
            pausedByPlayer = !pausedByPlayer;
            Apply();
        }
    }

    /// <summary>Freezes the map while a window is open (true), or lets it go again (false).</summary>
    public void SetHeld(bool hold)
    {
        held = hold;
        Apply();
    }

    private void Apply()
    {
        Time.timeScale = IsPaused ? 0f : 1f;
        if (banner != null)
        {
            banner.SetActive(pausedByPlayer);
        }
    }
}

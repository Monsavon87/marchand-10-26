using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

/// <summary>
/// Pause of the campaign map: the pause key freezes everything that moves (caravan, patrols) and shows a banner;
/// pressed again, the map goes on. A window that needs the map frozen while it is open (a town, the help page)
/// holds the pause through SetHeld, independently of the player's own pause and of the other windows: the map
/// goes on only when no window holds it any more.
/// </summary>
public class CampaignPause : MonoBehaviour
{
    [Tooltip("Keys are physical positions (US layout).")]
    [SerializeField] private Key pauseKey = Key.Space;
    [Tooltip("Shown while the player has paused the map.")]
    [SerializeField] private GameObject banner;

    private readonly HashSet<Object> holders = new HashSet<Object>();
    private bool pausedByPlayer;

    public bool IsPaused => pausedByPlayer || holders.Count > 0;

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

    /// <summary>
    /// Freezes the map while the 'owner' window is open (true), or stops that window's hold (false). Each window
    /// holds the pause on its own: closing one does not unfreeze the map while another is still open.
    /// </summary>
    public void SetHeld(Object owner, bool hold)
    {
        // Nothing changes when the window did not hold the pause (or already does): the time is left alone.
        bool changed = hold ? holders.Add(owner) : holders.Remove(owner);
        if (changed)
        {
            Apply();
        }
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

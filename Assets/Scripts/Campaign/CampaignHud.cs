using UnityEngine;

/// <summary>
/// Minimal campaign readout: date and time, and a message line for events (town reached, encounter).
/// Uses the built-in legacy Text (no font import needed). Read-only.
/// </summary>
public class CampaignHud : MonoBehaviour
{
    [SerializeField] private CampaignClock clock;
    [SerializeField] private CampaignTravel travel;
    [SerializeField] private CampaignEncounters encounters;
    [SerializeField] private UnityEngine.UI.Text clockLabel;
    [SerializeField] private UnityEngine.UI.Text messageLabel;
    [Tooltip("Seconds a message stays on screen.")]
    [SerializeField] private float messageDuration = 4f;
    [Tooltip("Seconds between two refreshes of the clock text.")]
    [SerializeField] private float refreshInterval = 0.1f;

    private float nextRefresh;
    private float hideMessageTime;

    private void Start()
    {
        if (clock == null || travel == null || encounters == null || clockLabel == null || messageLabel == null)
        {
            Debug.LogError("CampaignHud: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        messageLabel.text = string.Empty;
    }

    private void OnEnable()
    {
        if (travel != null)
        {
            travel.TownReached += OnTownReached;
        }

        if (encounters != null)
        {
            encounters.EncounterStarted += OnEncounterStarted;
        }
    }

    private void OnDisable()
    {
        if (travel != null)
        {
            travel.TownReached -= OnTownReached;
        }

        if (encounters != null)
        {
            encounters.EncounterStarted -= OnEncounterStarted;
        }
    }

    private void Update()
    {
        if (messageLabel.text.Length > 0 && Time.unscaledTime >= hideMessageTime)
        {
            messageLabel.text = string.Empty;
        }

        if (Time.unscaledTime < nextRefresh)
        {
            return;
        }

        nextRefresh = Time.unscaledTime + refreshInterval;
        clockLabel.text = "Jour " + clock.Day + "  -  " + clock.Hour.ToString("00") + "h" + clock.Minute.ToString("00");
    }

    private void OnTownReached(PointOfInterest town)
    {
        ShowMessage("Arrivé à " + town.DisplayName);
    }

    private void OnEncounterStarted(UnitTeam enemy)
    {
        ShowMessage("Rencontre !  Une patrouille ennemie bloque la route.");
    }

    private void ShowMessage(string text)
    {
        messageLabel.text = text;
        hideMessageTime = Time.unscaledTime + messageDuration;
    }
}

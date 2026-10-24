using UnityEngine;

/// <summary>
/// Minimal campaign readout: date and time, and a message line for events (town reached, encounter).
/// Uses the built-in legacy Text (no font import needed). Read-only.
/// </summary>
public class CampaignHud : MonoBehaviour
{
    [SerializeField] private CampaignClock clock;
    [Tooltip("Optional: writes what the map's time is doing (frozen, running...) next to the clock.")]
    [SerializeField] private CampaignPause pause;
    [SerializeField] private CampaignTravel travel;
    [SerializeField] private CampaignEncounters encounters;
    [SerializeField] private UnityEngine.UI.Text clockLabel;
    [SerializeField] private UnityEngine.UI.Text messageLabel;
    [Tooltip("Optional: says so when the group rests in a town.")]
    [SerializeField] private TownRest townRest;
    [Tooltip("Optional: shows the caravan's group (units and hit points).")]
    [SerializeField] private PartyRoster party;
    [SerializeField] private UnityEngine.UI.Text partyLabel;
    [Tooltip("Optional: shows the caravan's U.V. next to its group.")]
    [SerializeField] private Wallet wallet;
    [Tooltip("Seconds a message stays on screen.")]
    [SerializeField] private float messageDuration = 4f;
    [Tooltip("Seconds between two refreshes of the clock text.")]
    [SerializeField] private float refreshInterval = 0.1f;

    private readonly System.Text.StringBuilder partyText = new System.Text.StringBuilder(128);
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

        if (townRest != null)
        {
            townRest.Rested += OnRested;
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

        if (townRest != null)
        {
            townRest.Rested -= OnRested;
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
        clockLabel.text = "Jour " + clock.Day + "  -  " + clock.Hour.ToString("00") + "h" + clock.Minute.ToString("00")
            + (pause != null ? TimeStateText(pause.State) : string.Empty);
        if (party != null && partyLabel != null)
        {
            RefreshParty();
        }
    }

    private void RefreshParty()
    {
        System.Text.StringBuilder text = partyText.Clear().Append("Groupe : ");
        for (int i = 0; i < party.Members.Count; i++)
        {
            PartyMember member = party.Members[i];
            if (i > 0)
            {
                text.Append(", ");
            }

            text.Append(member.name).Append(' ').Append(member.StartingHealth);
            if (member.definition != null)
            {
                text.Append('/').Append(member.definition.MaxHealth);
            }
        }

        if (party.Members.Count == 0)
        {
            text.Append("personne");
        }

        if (wallet != null)
        {
            text.Append("     ").Append(MoneyFormat.Unit).Append(" : ").Append(MoneyFormat.Number(wallet.Balance));
        }

        partyLabel.text = text.ToString();
    }

    // What the map's time is doing, in a smaller size next to the clock. Nothing to say while it runs because of a move.
    private static string TimeStateText(CampaignTimeState state)
    {
        switch (state)
        {
            case CampaignTimeState.Frozen: return "   <size=20>Temps figé - T : laisser le temps passer</size>";
            case CampaignTimeState.Waiting: return "   <size=20>Le temps passe - T : s'arrêter</size>";
            default: return string.Empty;
        }
    }

    private void OnTownReached(PointOfInterest town)
    {
        ShowMessage("Arrivé à " + town.DisplayName);
    }

    private void OnRested(PointOfInterest town)
    {
        ShowMessage("Arrivé à " + town.DisplayName + " : votre groupe se repose et récupère tous ses PV.");
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

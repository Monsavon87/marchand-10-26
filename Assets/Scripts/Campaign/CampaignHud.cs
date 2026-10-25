using UnityEngine;

/// <summary>
/// Minimal campaign readout: date and time, and a message line for events (town reached, encounter).
/// Uses the built-in legacy Text (no font import needed). Read-only.
/// </summary>
public class CampaignHud : MonoBehaviour
{
    private const float LootMessageDuration = 8f;

    [SerializeField] private CampaignClock clock;
    [Tooltip("Optional: writes what the map's time is doing (frozen, running...) next to the clock.")]
    [SerializeField] private CampaignPause pause;
    [SerializeField] private CampaignTravel travel;
    [SerializeField] private CampaignEncounters encounters;
    [SerializeField] private UnityEngine.UI.Text clockLabel;
    [SerializeField] private UnityEngine.UI.Text messageLabel;
    [Tooltip("Optional: says so when the group rests in a town.")]
    [SerializeField] private TownRest townRest;
    [Tooltip("Optional: shows the length of the demo next to the day (Jour 12 / 100).")]
    [SerializeField] private DemoGoal goal;
    [Tooltip("Optional: says what a won fight brought (money, goods, what the cargo could not take).")]
    [SerializeField] private CampaignEncounterFlow encounterFlow;
    [Tooltip("Optional: shows the caravan's group (units and hit points).")]
    [SerializeField] private PartyRoster party;
    [SerializeField] private UnityEngine.UI.Text partyLabel;
    [Tooltip("Optional: says when the wages were paid, or not paid (debt).")]
    [SerializeField] private PartyPayroll payroll;
    [Tooltip("Optional: the caravan's orders. Says how many orders wait in its queue next to the clock.")]
    [SerializeField] private ActionController caravanActions;
    [Tooltip("Optional: shown when the map starts, to tell the player how to begin; it stays until the caravan's first order (needs caravanActions). Empty = nothing.")]
    [TextArea] [SerializeField] private string startMessage = string.Empty;
    [Tooltip("Seconds a message stays on screen.")]
    [SerializeField] private float messageDuration = 4f;
    [Tooltip("Seconds between two refreshes of the clock text.")]
    [SerializeField] private float refreshInterval = 0.1f;

    private readonly System.Text.StringBuilder partyText = new System.Text.StringBuilder(128);
    private float nextRefresh;
    private float hideMessageTime;
    private bool waitingFirstOrder;

    private void Start()
    {
        if (clock == null || travel == null || encounters == null || clockLabel == null || messageLabel == null)
        {
            Debug.LogError("CampaignHud: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        messageLabel.text = string.Empty;
        if (!string.IsNullOrEmpty(startMessage))
        {
            if (caravanActions == null)
            {
                Debug.LogError("CampaignHud: the start message needs caravanActions to know when the first order is given.", this);
                return;
            }

            // It stays on screen until the player gives the caravan its first order.
            ShowMessage(startMessage);
            hideMessageTime = float.MaxValue;
            waitingFirstOrder = true;
        }
    }

    private void OnFirstOrder(UnitAction action)
    {
        if (!waitingFirstOrder)
        {
            return;
        }

        waitingFirstOrder = false;
        if (messageLabel.text == startMessage)
        {
            messageLabel.text = string.Empty;
        }
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

        if (encounterFlow != null)
        {
            encounterFlow.EncounterResolved += OnEncounterResolved;
        }

        if (payroll != null)
        {
            payroll.Settled += OnPayrollSettled;
        }

        if (caravanActions != null)
        {
            caravanActions.ActionStarted += OnFirstOrder;
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

        if (encounterFlow != null)
        {
            encounterFlow.EncounterResolved -= OnEncounterResolved;
        }

        if (payroll != null)
        {
            payroll.Settled -= OnPayrollSettled;
        }

        if (caravanActions != null)
        {
            caravanActions.ActionStarted -= OnFirstOrder;
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
        clockLabel.text = "Jour " + (goal != null ? Mathf.Min(clock.Day, goal.Days) : clock.Day) + (goal != null ? " / " + goal.Days : string.Empty) + "  -  " + clock.Hour.ToString("00") + "h" + clock.Minute.ToString("00")
            + (pause != null ? TimeStateText(pause.State) : string.Empty)
            + (caravanActions != null && caravanActions.QueuedCount > 0 ? "   <size=20>File : " + caravanActions.QueuedCount + (caravanActions.QueuedCount > 1 ? " ordres" : " ordre") + " (X : annuler)</size>" : string.Empty);
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
        // With a town-arrival component (townRest), its own event writes the message, which may mention the hospital.
        if (townRest == null)
        {
            ShowMessage("Arrivé à " + town.DisplayName);
        }
    }

    private void OnRested(PointOfInterest town)
    {
        // The hospital is mentioned only when the group has lost hit points.
        bool wounded = party != null && party.HasWounded;
        ShowMessage("Arrivé à " + town.DisplayName + (wounded ? " : passez à l'hôpital pour soigner vos troupes." : "."));
    }

    private void OnEncounterResolved(EncounterData encounter)
    {
        if (encounter == null || !encounter.HasResult || encounter.Winner != Team.Player)
        {
            return;
        }

        string loot = LootMessages.Report(encounter.Loot.Uv, encounter.LootTaken, encounter.LootLost);
        if (loot.Length > 0)
        {
            ShowMessage("Victoire !  " + loot.Replace("\n", "  -  "), LootMessageDuration);
        }
    }

    private void OnPayrollSettled(PayrollReport report)
    {
        if (report.DebtAfter > 0)
        {
            ShowMessage("Salaires impayés : dette de " + MoneyFormat.WithUnit(report.DebtAfter));
        }
        else if (report.DebtBefore > 0)
        {
            ShowMessage("Salaires et dette payés : -" + MoneyFormat.WithUnit(report.Paid));
        }
        else if (report.Paid > 0)
        {
            ShowMessage("Salaires payés : -" + MoneyFormat.WithUnit(report.Paid));
        }
    }

    private void OnEncounterStarted(UnitTeam enemy)
    {
        ShowMessage("Rencontre !  Une patrouille ennemie bloque la route.");
    }

    private void ShowMessage(string text, float duration = 0f)
    {
        messageLabel.text = text;
        hideMessageTime = Time.unscaledTime + (duration > 0f ? duration : messageDuration);
    }
}

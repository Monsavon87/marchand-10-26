using UnityEngine;

/// <summary>
/// The caravan's group heals slowly by itself, on the road: each world day that begins (the campaign clock raises
/// DayStarted; nothing here has a timer) every unit regains a share of its hit points, up to a ceiling below its
/// maximum (see HealingRules and HealingSettings). Time that stands still (pause, a window, a combat) brings no day, so
/// it heals nothing.
/// </summary>
public class PartyRegeneration : MonoBehaviour
{
    [SerializeField] private HealingSettings settings;
    [SerializeField] private CampaignClock clock;
    [SerializeField] private PartyRoster party;

    private void Start()
    {
        if (settings == null || clock == null || party == null)
        {
            Debug.LogError("PartyRegeneration: a serialized reference is not assigned.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (clock != null)
        {
            clock.DayStarted += OnDayStarted;
        }
    }

    private void OnDisable()
    {
        if (clock != null)
        {
            clock.DayStarted -= OnDayStarted;
        }
    }

    private void OnDayStarted(int day)
    {
        party.RegenerateDay(settings);
    }
}

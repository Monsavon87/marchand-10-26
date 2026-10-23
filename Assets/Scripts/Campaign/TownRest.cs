using System;
using UnityEngine;

/// <summary>
/// Resting in town: when the caravan reaches a town, its whole group gets all its hit points back.
/// </summary>
public class TownRest : MonoBehaviour
{
    [SerializeField] private CampaignTravel travel;
    [SerializeField] private PartyRoster party;

    /// <summary>Raised after the group has rested in this town.</summary>
    public event Action<PointOfInterest> Rested = delegate { };

    private void Start()
    {
        if (travel == null || party == null)
        {
            Debug.LogError("TownRest: travel or party is not assigned.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (travel != null)
        {
            travel.TownReached += OnTownReached;
        }
    }

    private void OnDisable()
    {
        if (travel != null)
        {
            travel.TownReached -= OnTownReached;
        }
    }

    private void OnTownReached(PointOfInterest town)
    {
        party.HealAll();
        Rested(town);
    }
}

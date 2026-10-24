using System;
using UnityEngine;

/// <summary>
/// The group reaching a town: raises Rested, which opens the town window and the message of the map. A stay in town
/// heals nothing: hit points come back slowly on the road (see PartyRegeneration) or at the hospital, for U.V.
/// </summary>
public class TownRest : MonoBehaviour
{
    [SerializeField] private CampaignTravel travel;

    /// <summary>Raised when the group has reached this town.</summary>
    public event Action<PointOfInterest> Rested = delegate { };

    private void Start()
    {
        if (travel == null)
        {
            Debug.LogError("TownRest: travel is not assigned.", this);
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
        Rested(town);
    }
}

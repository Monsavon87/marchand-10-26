using System;
using UnityEngine;

/// <summary>
/// Order gate of the campaign map: a click on a town sends the caravan to its entrance instead of the clicked
/// point, and arriving there raises an event. Any other click is a plain move. It only gives orders to UnitMover.
/// </summary>
public class CampaignTravel : MoveOrderGate
{
    [SerializeField] private UnitMover caravan;

    private PointOfInterest destinationTown;

    /// <summary>Raised when the caravan reaches the entrance of the town it was sent to.</summary>
    public event Action<PointOfInterest> TownReached = delegate { };

    private void OnEnable()
    {
        if (caravan != null)
        {
            caravan.Arrived += OnArrived;
        }
    }

    private void OnDisable()
    {
        if (caravan != null)
        {
            caravan.Arrived -= OnArrived;
        }
    }

    private void Start()
    {
        if (caravan == null)
        {
            Debug.LogError("CampaignTravel: caravan is not assigned.", this);
            enabled = false;
        }
    }

    public override Vector3 ResolveDestination(Vector3 requested)
    {
        PointOfInterest town = FindTown(requested);
        return town != null ? town.Entrance : requested;
    }

    public override bool RequestMove(UnitMover mover, Vector3 destination)
    {
        if (mover != caravan)
        {
            mover.MoveTo(destination);
            return true;
        }

        destinationTown = FindTown(destination);
        mover.MoveTo(destinationTown != null ? destinationTown.Entrance : destination);
        return true;
    }

    public override void CancelOrder(UnitMover mover)
    {
        if (mover == caravan)
        {
            destinationTown = null;
        }

        mover.Stop();
    }

    private static PointOfInterest FindTown(Vector3 point)
    {
        System.Collections.Generic.IReadOnlyList<PointOfInterest> towns = PointOfInterest.All;
        for (int i = 0; i < towns.Count; i++)
        {
            if (towns[i].ContainsClick(point))
            {
                return towns[i];
            }
        }

        return null;
    }

    private void OnArrived()
    {
        if (destinationTown == null)
        {
            return;
        }

        PointOfInterest town = destinationTown;
        destinationTown = null;

        // The unit may have been cut short (closest reachable point): only a real arrival counts.
        Vector3 offset = caravan.GroundPosition - town.Entrance;
        offset.y = 0f;
        if (offset.magnitude <= town.ArrivalRadius)
        {
            TownReached(town);
        }
    }
}

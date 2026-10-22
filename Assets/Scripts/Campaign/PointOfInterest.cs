using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A place on the campaign map (a town). Pure data plus a click test: it never moves anything.
/// Clicking on it sends the caravan to its entrance (see CampaignTravel).
/// </summary>
public class PointOfInterest : MonoBehaviour
{
    private static readonly List<PointOfInterest> AllPoints = new List<PointOfInterest>();

    [SerializeField] private string displayName = "Town";
    [Tooltip("Where the caravan goes: a point on the NavMesh next to the town.")]
    [SerializeField] private Transform entrance;
    [Tooltip("A click inside this collider (seen from above) counts as a click on the town.")]
    [SerializeField] private Collider clickArea;
    [Tooltip("The caravan counts as arrived when it stops this close to the entrance, in meters.")]
    [SerializeField] private float arrivalRadius = 3f;

    /// <summary>Every enabled point of interest.</summary>
    public static IReadOnlyList<PointOfInterest> All => AllPoints;

    public string DisplayName => displayName;
    public Vector3 Entrance => entrance.position;
    public float ArrivalRadius => arrivalRadius;

    private void Awake()
    {
        if (entrance == null || clickArea == null)
        {
            Debug.LogError("PointOfInterest: entrance or clickArea is not assigned.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        AllPoints.Add(this);
    }

    private void OnDisable()
    {
        AllPoints.Remove(this);
    }

    /// <summary>True if the point, seen from above, is inside the town's click area.</summary>
    public bool ContainsClick(Vector3 point)
    {
        Bounds bounds = clickArea.bounds;
        return point.x >= bounds.min.x && point.x <= bounds.max.x
            && point.z >= bounds.min.z && point.z <= bounds.max.z;
    }
}

using UnityEngine;

/// <summary>
/// What an enemy band does on the campaign map each time it appears (at the start and at every return, see
/// BandRespawner): follow a route or wander. The choice is drawn with the weights below; a route made for a returning
/// band is drawn around its new starting point. Pure data.
/// </summary>
[CreateAssetMenu(fileName = "BandBehaviorSettings", menuName = "Marchand/Band Behavior Settings")]
public class BandBehaviorSettings : ScriptableObject
{
    [Header("Which behavior (drawn at each appearance)")]
    [Tooltip("Weight of 'follow a route'. 0 = never.")]
    [Min(0)] [SerializeField] private int routeWeight = 1;
    [Tooltip("Weight of 'wander'. 0 = never. At least one of the two weights must be above 0.")]
    [Min(0)] [SerializeField] private int wanderWeight = 1;

    [Header("Wandering")]
    [Tooltip("The band picks a point it can reach within this distance of where it stands, walks to it, waits, and starts again. Meters.")]
    [Min(5f)] [SerializeField] private float wanderRadius = 60f;
    [Tooltip("A wander hop is never shorter than this. Meters.")]
    [Min(0f)] [SerializeField] private float wanderMinHop = 15f;
    [Tooltip("Seconds spent at each point reached.")]
    [Min(0f)] [SerializeField] private float wanderWaitSeconds = 3f;

    [Header("Route made for a band that comes back")]
    [Min(2)] [SerializeField] private int routePointsMin = 3;
    [Min(2)] [SerializeField] private int routePointsMax = 5;
    [Tooltip("The points are drawn within this distance of the band's new starting point. Meters.")]
    [Min(5f)] [SerializeField] private float routeRadius = 40f;
    [Tooltip("Two following points of the route are at least this far apart. Meters.")]
    [Min(0f)] [SerializeField] private float routeMinSpacing = 10f;

    [Header("Spreading the bands over the map")]
    [Tooltip("A band never comes back closer than this to another band that is on the map. Meters.")]
    [Min(0f)] [SerializeField] private float minDistanceBetweenBands = 60f;
    [Tooltip("Valid starting points compared for a band that comes back: it takes the one farthest from the other bands.")]
    [Min(1)] [SerializeField] private int spawnCandidates = 5;
    [Tooltip("No starting point, route point or wandering point closer than this to the edge of the map. Meters.")]
    [Min(0f)] [SerializeField] private float edgeMargin = 25f;
    [Tooltip("A wandering band closer than this to the edge of the map is drawn back toward the middle. Meters.")]
    [Min(0f)] [SerializeField] private float edgePullZone = 60f;
    [Tooltip("In that zone, the next wandering point is the one nearest the middle among this many valid draws.")]
    [Min(1)] [SerializeField] private int edgePullSamples = 3;

    [Header("Draws")]
    [Tooltip("Tries to find one valid point before giving up.")]
    [Min(1)] [SerializeField] private int pickTries = 20;
    [Tooltip("Tries to find a starting point on the whole map for a band that comes back, before it waits one more day.")]
    [Min(1)] [SerializeField] private int spawnTries = 40;

    public int RouteWeight => routeWeight;
    public int WanderWeight => wanderWeight;
    public float WanderRadius => wanderRadius;
    public float WanderMinHop => Mathf.Min(wanderMinHop, wanderRadius);
    public float WanderWaitSeconds => wanderWaitSeconds;
    public int RoutePointsMin => routePointsMin;
    public int RoutePointsMax => Mathf.Max(routePointsMin, routePointsMax);
    public float RouteRadius => routeRadius;
    public float RouteMinSpacing => routeMinSpacing;
    public float MinDistanceBetweenBands => minDistanceBetweenBands;
    public int SpawnCandidates => spawnCandidates;
    public float EdgeMargin => edgeMargin;
    public float EdgePullZone => edgePullZone;
    public int EdgePullSamples => edgePullSamples;
    public int PickTries => pickTries;
    public int SpawnTries => spawnTries;

    private void OnValidate()
    {
        if (routeWeight + wanderWeight <= 0)
        {
            Debug.LogWarning("BandBehaviorSettings '" + name + "': both weights are 0, bands will never move on their own.", this);
        }
    }
}

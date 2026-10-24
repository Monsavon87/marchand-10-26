using UnityEngine;

/// <summary>
/// How a beaten enemy band comes back on the campaign map (see BandRespawner): after a number of world days drawn
/// between a minimum and a maximum, at its starting point, never close to the caravan nor, as a rule, in the view of the
/// player. Pure data.
/// </summary>
[CreateAssetMenu(fileName = "BandRespawnSettings", menuName = "Marchand/Band Respawn Settings")]
public class BandRespawnSettings : ScriptableObject
{
    [Header("When")]
    [Tooltip("Fewest world days between the defeat of a band and its return.")]
    [Min(1)] [SerializeField] private int minDays = 4;
    [Tooltip("Most world days between the defeat of a band and its return. The delay is drawn between the two (same number = fixed).")]
    [Min(1)] [SerializeField] private int maxDays = 7;
    [Tooltip("0 = a different draw each game; any other number = always the same draws (tests).")]
    [SerializeField] private int seed;

    [Header("Where it may appear")]
    [Tooltip("A band never appears closer to the caravan than this, in meters.")]
    [Min(0f)] [SerializeField] private float minDistanceFromCaravan = 50f;
    [Tooltip("Margin around the screen, as a share of its size, inside which a point counts as seen by the player.")]
    [Range(0f, 0.5f)] [SerializeField] private float viewMargin = 0.1f;
    [Tooltip("A band is kept out of the player's view; but when it has been held back this many days (a camera zoomed right out sees everything), the view no longer matters and only the distance does.")]
    [Min(0)] [SerializeField] private int daysBeforeIgnoringView = 2;

    public int MinDays => minDays;
    public int MaxDays => Mathf.Max(minDays, maxDays);
    public int Seed => seed;
    public float MinDistanceFromCaravan => minDistanceFromCaravan;
    public float ViewMargin => viewMargin;
    public int DaysBeforeIgnoringView => daysBeforeIgnoringView;

    private void OnValidate()
    {
        if (maxDays < minDays)
        {
            Debug.LogWarning("BandRespawnSettings '" + name + "': the maximum delay is below the minimum.", this);
        }
    }
}

using UnityEngine;

/// <summary>
/// Campaign time. The map is a miniature: the distance the caravan walks is converted into game time
/// (minutes per meter, adjustable). Slow terrain makes the same distance take longer, fast terrain (roads) shorter.
/// Time only advances while the caravan moves. Read-only for everybody else.
/// </summary>
public class CampaignClock : MonoBehaviour
{
    private const float MinutesPerHour = 60f;
    private const float MinutesPerDay = 1440f;

    [SerializeField] private UnitMover caravan;
    [SerializeField] private UnitTerrainEffects caravanTerrain;
    [Tooltip("Game minutes that pass per meter walked on plain terrain (speed multiplier 1).")]
    [SerializeField] private float minutesPerMeter = 10f;
    [Tooltip("Hour of the day (0-24) when the campaign starts, on day 1.")]
    [SerializeField] private float startHour = 8f;

    private float totalMinutes;
    private Vector3 lastPosition;

    /// <summary>Game minutes elapsed since day 1, 00:00.</summary>
    public float TotalMinutes => totalMinutes;
    public int Day => 1 + (int)(totalMinutes / MinutesPerDay);
    public int Hour => (int)(totalMinutes % MinutesPerDay / MinutesPerHour);
    public int Minute => (int)(totalMinutes % MinutesPerHour);

    private void Start()
    {
        if (caravan == null || caravanTerrain == null)
        {
            Debug.LogError("CampaignClock: caravan or caravanTerrain is not assigned.", this);
            enabled = false;
            return;
        }

        totalMinutes = startHour * MinutesPerHour;
        lastPosition = caravan.transform.position;
    }

    private void Update()
    {
        Vector3 position = caravan.transform.position;
        if (caravan.IsMoving)
        {
            Vector3 step = position - lastPosition;
            step.y = 0f;
            float multiplier = Mathf.Max(0.01f, caravanTerrain.CurrentSpeedMultiplier);
            totalMinutes += step.magnitude * minutesPerMeter / multiplier;
        }

        lastPosition = position;
    }
}

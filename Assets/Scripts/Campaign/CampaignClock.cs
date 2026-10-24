using UnityEngine;

/// <summary>
/// Campaign time. The map is a miniature: the world's time is converted into game time at the pace of the caravan's
/// walk (minutes per meter at its base speed, adjustable), so a day passes as it would if the caravan walked
/// nonstop. It follows the world's time, not the caravan's distance: it runs when the world runs (the caravan has
/// an action, or the player lets time pass) and stands still when the world is frozen or paused (see
/// CampaignPause). Slow terrain slows the caravan down but not the calendar. Read-only for everybody else.
/// </summary>
public class CampaignClock : MonoBehaviour
{
    private const float MinutesPerHour = 60f;
    private const float MinutesPerDay = 1440f;

    [SerializeField] private UnitMover caravan;
    [Tooltip("Game minutes that pass per meter the caravan walks at its base speed. With the speed, it sets the pace of the world's time.")]
    [SerializeField] private float minutesPerMeter = 10f;
    [Tooltip("Hour of the day (0-24) when the campaign starts, on day 1.")]
    [SerializeField] private float startHour = 8f;

    private float totalMinutes;
    private float minutesPerSecond;

    /// <summary>Game minutes elapsed since day 1, 00:00.</summary>
    public float TotalMinutes => totalMinutes;
    public int Day => 1 + (int)(totalMinutes / MinutesPerDay);
    public int Hour => (int)(totalMinutes % MinutesPerDay / MinutesPerHour);
    public int Minute => (int)(totalMinutes % MinutesPerHour);

    private void Start()
    {
        if (caravan == null)
        {
            Debug.LogError("CampaignClock: caravan is not assigned.", this);
            enabled = false;
            return;
        }

        minutesPerSecond = minutesPerMeter * caravan.BaseSpeed;
        if (minutesPerSecond <= 0f)
        {
            Debug.LogError("CampaignClock: the caravan's base speed or the minutes per meter is not more than 0: the time would never pass.", this);
            enabled = false;
            return;
        }

        totalMinutes = startHour * MinutesPerHour;
    }

    private void Update()
    {
        // Time.deltaTime is 0 while the world is frozen or paused, so the clock stands still with it.
        totalMinutes += Time.deltaTime * minutesPerSecond;
    }
}

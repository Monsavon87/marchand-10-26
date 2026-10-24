using UnityEngine;

/// <summary>
/// How the markets move from one day to the next. Pure data, editable in the Inspector; read by the economy ticker.
/// </summary>
[CreateAssetMenu(fileName = "EconomySettings", menuName = "Marchand/Economy/Economy Settings")]
public class EconomySettings : ScriptableObject
{
    [Tooltip("Each day, after production and consumption, a stock comes back toward its target by this percent of the gap (at least 1 unit, never past the target). The higher, the faster a bought-out or flooded market recovers.")]
    [Range(1, 100)] [SerializeField] private int reversionPercent = 20;

    public int ReversionPercent => reversionPercent;

    /// <summary>Writes a warning for each invalid value, and returns how many there were.</summary>
    public int Validate()
    {
        if (reversionPercent < 1 || reversionPercent > 100)
        {
            Debug.LogWarning("EconomySettings: the reversion percent must be between 1 and 100 (it is " + reversionPercent + ").", this);
            return 1;
        }

        return 0;
    }

    private void OnValidate()
    {
        Validate();
    }
}

using UnityEngine;

/// <summary>
/// How much a convoy can carry: a limit in mass (kg) and a limit in volume (L). Both count: a cargo is refused as
/// soon as one of them would be exceeded. Pure data, editable in the Inspector.
/// </summary>
[CreateAssetMenu(fileName = "CargoCapacity", menuName = "Marchand/Economy/Cargo Capacity")]
public class CargoCapacity : ScriptableObject
{
    [Tooltip("Most mass the convoy can carry, in kilograms. More than 0.")]
    [Min(0.001f)] [SerializeField] private float maxMass = 600f;
    [Tooltip("Most volume the convoy can carry, in liters. More than 0.")]
    [Min(0.001f)] [SerializeField] private float maxVolume = 1000f;

    public float MaxMass => maxMass;
    public float MaxVolume => maxVolume;

    /// <summary>Writes a warning for each invalid value, and returns how many there were.</summary>
    public int Validate()
    {
        int problems = 0;
        if (maxMass <= 0f)
        {
            Debug.LogWarning("CargoCapacity: the mass limit must be more than 0 (it is " + maxMass + ").", this);
            problems++;
        }

        if (maxVolume <= 0f)
        {
            Debug.LogWarning("CargoCapacity: the volume limit must be more than 0 (it is " + maxVolume + ").", this);
            problems++;
        }

        return problems;
    }

    private void OnValidate()
    {
        Validate();
    }
}

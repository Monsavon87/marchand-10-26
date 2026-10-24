using UnityEngine;

/// <summary>What a good is mostly: used to group goods (in a market list, for instance).</summary>
public enum GoodCategory
{
    Vivres,
    Textile,
    Metal,
    Outil,
    Luxe,
    MatierePremiere
}

/// <summary>
/// Data describing a type of good that can be bought and sold (wheat, wine, iron...). Pure data, editable in the
/// Inspector: nothing here changes while the game runs. The price is the base price of ONE unit of measure, in U.V.
/// (the one currency of the game, always a whole number). Mass is in kilograms and volume in liters, per unit.
/// </summary>
[CreateAssetMenu(fileName = "GoodDefinition", menuName = "Marchand/Economy/Good Definition")]
public class GoodDefinition : ScriptableObject
{
    [Tooltip("Unique short name, lowercase, no spaces (ble, vin, fer...). The catalog finds the good by it.")]
    [SerializeField] private string id = "good";
    [SerializeField] private string displayName = "Marchandise";
    [SerializeField] private GoodCategory category;

    [Header("Value")]
    [Tooltip("Base price of one unit of measure, in U.V. Whole number, more than 0.")]
    [Min(1)] [SerializeField] private int basePrice = 100;
    [Tooltip("A stable good keeps its base price: it does not follow supply and demand (wheat, monetary metals).")]
    [SerializeField] private bool stablePrice;

    [Header("Measure")]
    [Tooltip("What one unit is called: muid, amphore, balle, sac...")]
    [SerializeField] private string unit = "unité";
    [Tooltip("Mass of one unit, in kilograms. More than 0.")]
    [Min(0.001f)] [SerializeField] private float massPerUnit = 1f;
    [Tooltip("Volume of one unit, in liters. More than 0.")]
    [Min(0.001f)] [SerializeField] private float volumePerUnit = 1f;

    public string Id => id;
    public string DisplayName => displayName;
    public GoodCategory Category => category;
    public int BasePrice => basePrice;
    public bool StablePrice => stablePrice;
    public string Unit => unit;
    public float MassPerUnit => massPerUnit;
    public float VolumePerUnit => volumePerUnit;

    /// <summary>Writes a warning for each invalid value, and returns how many there were.</summary>
    public int Validate()
    {
        int problems = 0;
        if (string.IsNullOrWhiteSpace(id))
        {
            Debug.LogWarning("GoodDefinition: the identifier is empty.", this);
            problems++;
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            Debug.LogWarning("GoodDefinition '" + id + "': the displayed name is empty.", this);
            problems++;
        }

        if (string.IsNullOrWhiteSpace(unit))
        {
            Debug.LogWarning("GoodDefinition '" + id + "': the unit of measure is empty.", this);
            problems++;
        }

        if (basePrice <= 0)
        {
            Debug.LogWarning("GoodDefinition '" + id + "': the base price must be more than 0 (it is " + basePrice + ").", this);
            problems++;
        }

        if (massPerUnit <= 0f)
        {
            Debug.LogWarning("GoodDefinition '" + id + "': the mass per unit must be more than 0 (it is " + massPerUnit + ").", this);
            problems++;
        }

        if (volumePerUnit <= 0f)
        {
            Debug.LogWarning("GoodDefinition '" + id + "': the volume per unit must be more than 0 (it is " + volumePerUnit + ").", this);
            problems++;
        }

        return problems;
    }

    private void OnValidate()
    {
        Validate();
    }
}

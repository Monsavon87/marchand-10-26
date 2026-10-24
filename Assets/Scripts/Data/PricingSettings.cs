using UnityEngine;

/// <summary>
/// The numbers behind the prices: how the price of a good follows its stock, how far it can go, and the margin
/// between what the player pays and what the player is paid. Pure data, editable in the Inspector. Read by the
/// pricing rules; nothing here changes while the game runs.
/// </summary>
[CreateAssetMenu(fileName = "PricingSettings", menuName = "Marchand/Economy/Pricing Settings")]
public class PricingSettings : ScriptableObject
{
    [Tooltip("The price factor by stock level: X = stock / target stock, Y = factor applied to the base price. It must go down: the rarer, the dearer.")]
    [SerializeField] private AnimationCurve factorCurve = DefaultCurve();
    [Tooltip("The factor never goes under this, whatever the curve says. More than 0.")]
    [Min(0.01f)] [SerializeField] private float minFactor = 0.5f;
    [Tooltip("The factor never goes over this, whatever the curve says.")]
    [Min(0.01f)] [SerializeField] private float maxFactor = 2f;
    [Tooltip("Margin between the buy price and the sell price, in percent of the price: half is added when the player buys, half is taken when the player sells. More than 0, so a round trip always loses.")]
    [Range(0.1f, 90f)] [SerializeField] private float spreadPercent = 10f;

    public AnimationCurve FactorCurve => factorCurve;
    public float MinFactor => minFactor;
    public float MaxFactor => maxFactor;
    public float SpreadPercent => spreadPercent;
    /// <summary>The share of the price added on a purchase and taken on a sale (half of the spread), as a fraction.</summary>
    public double HalfSpread => spreadPercent / 200.0;

    /// <summary>Writes a warning for each invalid value, and returns how many there were.</summary>
    public int Validate()
    {
        int problems = 0;
        if (factorCurve == null || factorCurve.length < 2)
        {
            Debug.LogWarning("PricingSettings: the factor curve needs at least 2 points.", this);
            return 1;
        }

        if (minFactor <= 0f || maxFactor < minFactor)
        {
            Debug.LogWarning("PricingSettings: the factors must be more than 0, with the minimum not above the maximum (" + minFactor + " / " + maxFactor + ").", this);
            problems++;
        }

        if (spreadPercent <= 0f || spreadPercent >= 100f)
        {
            Debug.LogWarning("PricingSettings: the spread must be more than 0 and less than 100 percent (it is " + spreadPercent + ").", this);
            problems++;
        }

        // The curve must not go up anywhere: sample it over its whole width.
        float end = factorCurve[factorCurve.length - 1].time;
        float previous = factorCurve.Evaluate(0f);
        for (float x = 0.05f; x <= end; x += 0.05f)
        {
            float value = factorCurve.Evaluate(x);
            if (value > previous + 0.0001f)
            {
                Debug.LogWarning("PricingSettings: the factor curve goes up near a ratio of " + x.ToString("0.##") + ": a rarer good must never be cheaper.", this);
                problems++;
                break;
            }

            previous = value;
        }

        return problems;
    }

    private void OnValidate()
    {
        Validate();
    }

    // Straight lines between the points (each key's tangents follow its segments), so the curve never overshoots.
    private static AnimationCurve DefaultCurve()
    {
        float[] x = { 0f, 0.25f, 0.5f, 0.75f, 1f, 1.5f, 2f, 3f, 4f };
        float[] y = { 2f, 1.6f, 1.35f, 1.15f, 1f, 0.8f, 0.65f, 0.55f, 0.5f };
        var curve = new AnimationCurve();
        for (int i = 0; i < x.Length; i++)
        {
            float before = i > 0 ? (y[i] - y[i - 1]) / (x[i] - x[i - 1]) : (y[1] - y[0]) / (x[1] - x[0]);
            float after = i < x.Length - 1 ? (y[i + 1] - y[i]) / (x[i + 1] - x[i]) : before;
            curve.AddKey(new Keyframe(x[i], y[i], before, after));
        }

        return curve;
    }
}

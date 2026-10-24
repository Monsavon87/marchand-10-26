using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class WealthTier
{
    [Tooltip("Word shown to the player (Maigre, Modeste...).")]
    public string label = "Modeste";
    [Tooltip("The tier covers values below this one (in U.V.). Leave the last tier at 0: it covers everything above.")]
    [Min(0)] public int below;
}

/// <summary>
/// The words that tell the player how rich a band's loot is, without saying what it holds. The tiers are listed from
/// the poorest to the richest; the last one has no upper limit. Pure data.
/// </summary>
[CreateAssetMenu(fileName = "LootWealthScale", menuName = "Marchand/Economy/Loot Wealth Scale")]
public class LootWealthScale : ScriptableObject
{
    [SerializeField] private List<WealthTier> tiers = new List<WealthTier>();

    public IReadOnlyList<WealthTier> Tiers => tiers;

    /// <summary>The word for a loot of this value (empty when the scale has no tier).</summary>
    public string Describe(int value)
    {
        for (int i = 0; i < tiers.Count; i++)
        {
            bool last = i == tiers.Count - 1;
            if (last || value < tiers[i].below)
            {
                return tiers[i].label;
            }
        }

        return string.Empty;
    }

    private void OnValidate()
    {
        for (int i = 1; i < tiers.Count - 1; i++)
        {
            if (tiers[i].below <= tiers[i - 1].below)
            {
                Debug.LogWarning("LootWealthScale '" + name + "': the tiers must be listed from the poorest to the richest.", this);
                return;
            }
        }
    }
}

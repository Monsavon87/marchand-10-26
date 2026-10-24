using UnityEngine;

/// <summary>
/// How the caravan's group heals: slowly by itself while the world's time runs, up to a ceiling (so the hospital stays
/// useful for the rest), and at the hospital of a town for U.V. per hit point given back. Pure data.
/// </summary>
[CreateAssetMenu(fileName = "HealingSettings", menuName = "Marchand/Healing Settings")]
public class HealingSettings : ScriptableObject
{
    [Header("Natural healing (only while the world's time runs)")]
    [Tooltip("Share of its maximum hit points each unit regains per world day, in percent. At least 1 hit point a day.")]
    [Range(1, 100)] [SerializeField] private int regenPercentPerDay = 8;
    [Tooltip("Natural healing stops at this share of the maximum hit points, in percent: the rest is for the hospital.")]
    [Range(1, 100)] [SerializeField] private int regenCapPercent = 80;

    [Header("Hospital")]
    [Tooltip("U.V. paid for each hit point given back.")]
    [Min(0)] [SerializeField] private int pricePerHitPoint = 25;

    public int RegenPercentPerDay => regenPercentPerDay;
    public int RegenCapPercent => regenCapPercent;
    public int PricePerHitPoint => pricePerHitPoint;
}

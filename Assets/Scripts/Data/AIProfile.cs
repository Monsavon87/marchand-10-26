using UnityEngine;

/// <summary>How an AI unit picks the enemy it fights.</summary>
public enum AITargetRule
{
    /// <summary>The closest enemy.</summary>
    Nearest,
    /// <summary>The enemy with the fewest hit points left.</summary>
    Weakest,
    /// <summary>The enemy that hurts the most (weapon damage per second).</summary>
    MostDangerous
}

/// <summary>
/// How an AI unit behaves in combat: who it targets, when it falls back, how much it cares about exposing itself.
/// Pure data, editable in the Inspector. "Aggressive" never falls back; "Cautious" does.
/// </summary>
[CreateAssetMenu(fileName = "AIProfile", menuName = "Marchand/AI Profile")]
public class AIProfile : ScriptableObject
{
    [SerializeField] private string displayName = "Profile";

    [Header("Targeting")]
    [Tooltip("The main criterion for choosing a target.")]
    [SerializeField] private AITargetRule targetRule = AITargetRule.Nearest;
    [Tooltip("How much an enemy whose flank or back the unit can reach is preferred (0 = ignored, 1 = as important as the rule).")]
    [Range(0f, 1f)] [SerializeField] private float weakPointWeight = 0.3f;
    [Tooltip("How much an enemy that is attacking one of the unit's allies is preferred (0 = ignored).")]
    [Range(0f, 1f)] [SerializeField] private float protectAlliesWeight = 0.2f;
    [Tooltip("A melee unit walks around to the flank or back of its target when that does not cost too much of a detour.")]
    [SerializeField] private bool flankingManeuver = true;
    [Tooltip("The unit only changes target if the new one scores this much better (a bigger number = more stubborn).")]
    [Min(0f)] [SerializeField] private float switchTargetMargin = 0.3f;
    [Tooltip("Seconds before the unit may change target again.")]
    [Min(0f)] [SerializeField] private float switchTargetDelay = 3f;

    [Header("Retreat")]
    [Tooltip("The unit falls back when its health falls to this fraction (0.35 = 35 %). 0 = never falls back.")]
    [Range(0f, 1f)] [SerializeField] private float retreatBelowHealth;
    [Tooltip("A unit that fell back fights again once its health is back above this fraction (when healing exists).")]
    [Range(0f, 1f)] [SerializeField] private float returnAboveHealth = 0.7f;
    [Tooltip("How many times a unit moves to get away before it stops and fights (so a chase cannot go on forever).")]
    [Min(1)] [SerializeField] private int maxRetreatMoves = 4;

    [Header("Positioning")]
    [Tooltip("The unit avoids standing at the flank or back of enemies (it tries not to expose itself).")]
    [SerializeField] private bool avoidExposedPositions;
    [Tooltip("Ranged units: they back away when an enemy comes closer than this fraction of their maximum range.")]
    [Range(0f, 1f)] [SerializeField] private float comfortDistanceFraction = 0.4f;
    [Tooltip("Ranged units: how many times in a row they back away before they stop and shoot anyway.")]
    [Min(0)] [SerializeField] private int maxKiteMoves = 2;

    [Header("Timing")]
    [Tooltip("Seconds between two decisions.")]
    [Min(0.1f)] [SerializeField] private float decisionInterval = 0.5f;

    public string DisplayName => displayName;
    public AITargetRule TargetRule => targetRule;
    public float WeakPointWeight => weakPointWeight;
    public float ProtectAlliesWeight => protectAlliesWeight;
    public bool FlankingManeuver => flankingManeuver;
    public float SwitchTargetMargin => switchTargetMargin;
    public float SwitchTargetDelay => switchTargetDelay;
    public float RetreatBelowHealth => retreatBelowHealth;
    public float ReturnAboveHealth => returnAboveHealth;
    public int MaxRetreatMoves => maxRetreatMoves;
    public bool AvoidExposedPositions => avoidExposedPositions;
    public float ComfortDistanceFraction => comfortDistanceFraction;
    public int MaxKiteMoves => maxKiteMoves;
    public float DecisionInterval => decisionInterval;

    private void OnValidate()
    {
        // Coming back to the fight at or below the fall-back threshold would make the unit flip-flop.
        if (returnAboveHealth < retreatBelowHealth)
        {
            returnAboveHealth = retreatBelowHealth;
        }
    }
}

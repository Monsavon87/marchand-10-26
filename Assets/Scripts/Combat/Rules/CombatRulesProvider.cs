using UnityEngine;

/// <summary>
/// Puts the rules in the scene: holds the settings assets and hands out the ICombatRules everybody uses.
/// To change the rules system, return another ICombatRules here; nothing else needs to know.
/// </summary>
public class CombatRulesProvider : MonoBehaviour
{
    [SerializeField] private CombatRulesSettings settings;
    [SerializeField] private AttackZoneSettings zoneSettings;

    private ICombatRules rules;

    public ICombatRules Rules
    {
        get
        {
            if (rules == null && settings != null && zoneSettings != null)
            {
                rules = new CombatRules(settings, zoneSettings);
            }

            return rules;
        }
    }

    /// <summary>The zone angles, for drawing the zones (the rules themselves stay behind ICombatRules).</summary>
    public AttackZoneSettings ZoneSettings => zoneSettings;

    private void Awake()
    {
        if (settings == null || zoneSettings == null)
        {
            Debug.LogError("CombatRulesProvider: settings or zoneSettings is not assigned.", this);
        }
    }
}

using UnityEngine;

/// <summary>
/// Puts the rules in the scene: holds the settings asset and hands out the ICombatRules everybody uses.
/// To change the rules system, return another ICombatRules here; nothing else needs to know.
/// </summary>
public class CombatRulesProvider : MonoBehaviour
{
    [SerializeField] private CombatRulesSettings settings;

    private ICombatRules rules;

    public ICombatRules Rules
    {
        get
        {
            if (rules == null && settings != null)
            {
                rules = new CombatRules(settings);
            }

            return rules;
        }
    }

    private void Awake()
    {
        if (settings == null)
        {
            Debug.LogError("CombatRulesProvider: settings is not assigned.", this);
        }
    }
}

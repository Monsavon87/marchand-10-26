using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The combat journal on screen: one readable line per attack, the latest at the bottom.
/// Listens to CombatEvents. Uses the built-in legacy Text (no font import needed).
/// </summary>
public class CombatLog : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Text label;
    [Tooltip("How many lines are kept on screen.")]
    [SerializeField] private int maxLines = 8;

    private readonly List<string> lines = new List<string>();

    private void Start()
    {
        if (label == null)
        {
            Debug.LogError("CombatLog: label is not assigned.", this);
            enabled = false;
            return;
        }

        label.text = string.Empty;
    }

    private void OnEnable()
    {
        CombatEvents.AttackResolved += OnAttackResolved;
    }

    private void OnDisable()
    {
        CombatEvents.AttackResolved -= OnAttackResolved;
    }

    /// <summary>Adds a line to the journal.</summary>
    public void Add(string line)
    {
        lines.Add(line);
        while (lines.Count > maxLines)
        {
            lines.RemoveAt(0);
        }

        label.text = string.Join("\n", lines);
    }

    private void OnAttackResolved(AttackReport report)
    {
        AttackResult r = report.Result;
        string zone = AttackZones.Name(r.Zone);
        string line = report.AttackerName + " frappe " + report.DefenderName + " (" + zone + ") : jet " + r.Roll
            + " + " + r.AttackBonus;
        if (r.ZoneHitBonus != 0)
        {
            line += " + " + r.ZoneHitBonus + " (" + zone + ")";
        }

        line += " = " + r.Total + " contre " + r.Threshold;
        if (r.Hit)
        {
            line += " > touché, " + r.WeaponDamage;
            if (r.ZoneDamageBonus != 0)
            {
                line += " + " + r.ZoneDamageBonus + " (" + zone + ")";
            }

            line += " - " + r.Armor + " = " + r.Damage + " dégâts (PV "
                + report.DefenderHealth + "/" + report.DefenderMaxHealth + ")";
        }
        else
        {
            line += " > raté";
        }

        if (report.DefenderKilled)
        {
            line += " - " + report.DefenderName + " est mort.";
        }

        Add(line);
    }
}

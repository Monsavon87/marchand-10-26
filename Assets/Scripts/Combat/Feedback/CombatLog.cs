using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The combat journal on screen: one readable line per attack, the latest at the bottom, plus the big moments of
/// the combat (start, automatic pauses, end). Listens to CombatEvents and, when set, to the combat manager. Uses the built-in legacy Text (no font import needed).
/// </summary>
public class CombatLog : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Text label;
    [Tooltip("Optional: adds the start, the automatic pauses and the end of the combat to the journal.")]
    [SerializeField] private CombatManager combat;
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
        CombatEvents.Note += Add;
        if (combat != null)
        {
            combat.CombatStarted += OnCombatStarted;
            combat.CombatEnded += OnCombatEnded;
            combat.PauseChanged += OnPauseChanged;
        }
    }

    private void OnDisable()
    {
        CombatEvents.AttackResolved -= OnAttackResolved;
        CombatEvents.Note -= Add;
        if (combat != null)
        {
            combat.CombatStarted -= OnCombatStarted;
            combat.CombatEnded -= OnCombatEnded;
            combat.PauseChanged -= OnPauseChanged;
        }
    }

    private void OnCombatStarted()
    {
        Add("- Début du combat : " + combat.PlayersAlive + " contre " + combat.EnemiesAlive + " -");
    }

    private void OnCombatEnded(Team? winner)
    {
        Add(winner.HasValue
            ? "- Fin du combat : victoire du camp " + (winner.Value == Team.Player ? "du joueur" : "ennemi") + " -"
            : "- Fin du combat : match nul -");
    }

    private void OnPauseChanged(bool paused)
    {
        if (paused && combat.PauseCause == PauseCause.AllyDied)
        {
            Add("- Pause automatique : un allié est tombé -");
        }
        else if (paused && combat.PauseCause == PauseCause.EnemySighted)
        {
            Add("- Pause automatique : un ennemi est en vue -");
        }
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
        string line = report.AttackerName + (r.IsRanged ? " tire sur " : " frappe ") + report.DefenderName
            + " (" + zone + (r.IsRanged ? ", distance " + r.Distance.ToString("F1") + " m" : string.Empty)
            + ") : jet " + r.Roll + " + " + r.AttackBonus;
        if (r.ZoneHitBonus != 0)
        {
            line += " + " + r.ZoneHitBonus + " (" + zone + ")";
        }

        if (r.RangeHitModifier != 0)
        {
            line += (r.RangeHitModifier > 0 ? " + " : " - ") + Mathf.Abs(r.RangeHitModifier) + " (distance)";
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

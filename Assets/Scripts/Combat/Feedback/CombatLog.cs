using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The combat journal on screen: one readable line per attack, the latest at the bottom, plus the big moments of
/// the combat (start, automatic pauses, end). Listens to CombatEvents and, when set, to the combat manager.
/// Lines are colored: hits and misses of the player's side and of the enemy's, notes, combat events.
/// In a scroll view it follows the latest line unless the player has scrolled up to read.
/// Uses the built-in legacy Text (no font import needed).
/// </summary>
public class CombatLog : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Text label;
    [Tooltip("Optional: adds the start, the automatic pauses and the end of the combat to the journal.")]
    [SerializeField] private CombatManager combat;
    [Tooltip("Optional: the scroll view the label is in. The journal follows the latest line.")]
    [SerializeField] private UnityEngine.UI.ScrollRect scrollRect;
    [Tooltip("How many lines are kept.")]
    [SerializeField] private int maxLines = 60;
    [SerializeField] private Color playerHitColor = new Color(0.55f, 0.91f, 0.55f);
    [SerializeField] private Color enemyHitColor = new Color(1f, 0.54f, 0.5f);
    [SerializeField] private Color missColor = new Color(0.78f, 0.78f, 0.78f);
    [SerializeField] private Color noteColor = new Color(1f, 0.88f, 0.54f);
    [SerializeField] private Color eventColor = new Color(0.62f, 0.78f, 1f);

    private readonly List<string> lines = new List<string>();
    private bool followLatest;
    private bool scrollPending;

    private void Start()
    {
        if (label == null)
        {
            Debug.LogError("CombatLog: label is not assigned.", this);
            enabled = false;
            return;
        }

        // Lines written before this script started (the start of the combat) must not be wiped.
        if (lines.Count == 0)
        {
            label.text = string.Empty;
        }
    }

    private void OnEnable()
    {
        CombatEvents.AttackResolved += OnAttackResolved;
        CombatEvents.Note += OnNote;
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
        CombatEvents.Note -= OnNote;
        if (combat != null)
        {
            combat.CombatStarted -= OnCombatStarted;
            combat.CombatEnded -= OnCombatEnded;
            combat.PauseChanged -= OnPauseChanged;
        }
    }

    private void OnCombatStarted()
    {
        Add("- Début du combat : " + combat.PlayersAlive + " contre " + combat.EnemiesAlive + " -", eventColor);
    }

    private void OnCombatEnded(Team? winner)
    {
        Add(winner.HasValue
            ? "- Fin du combat : victoire du camp " + (winner.Value == Team.Player ? "du joueur" : "ennemi") + " -"
            : "- Fin du combat : match nul -", eventColor);
    }

    private void OnPauseChanged(bool paused)
    {
        if (paused && combat.PauseCause == PauseCause.AllyDied)
        {
            Add("- Pause automatique : un allié est tombé -", eventColor);
        }
        else if (paused && combat.PauseCause == PauseCause.EnemySighted)
        {
            Add("- Pause automatique : un ennemi est en vue -", eventColor);
        }
    }

    private void OnNote(string text)
    {
        Add(text, noteColor);
    }

    /// <summary>Adds a line to the journal, in this color.</summary>
    public void Add(string line, Color color)
    {
        // The view follows the latest line only if it was already at the bottom (the player may be reading above).
        followLatest = scrollRect == null || scrollRect.verticalNormalizedPosition <= 0.02f;
        lines.Add("<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + line + "</color>");
        while (lines.Count > maxLines)
        {
            lines.RemoveAt(0);
        }

        label.text = string.Join("\n", lines);
        scrollPending = followLatest && scrollRect != null;
    }

    private void LateUpdate()
    {
        if (scrollPending)
        {
            scrollPending = false;
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
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

        Add(line, !r.Hit ? missColor : report.AttackerTeam == Team.Player ? playerHitColor : enemyHitColor);
    }
}

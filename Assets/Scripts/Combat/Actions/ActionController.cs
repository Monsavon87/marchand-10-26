using System;
using UnityEngine;

/// <summary>
/// The single entry point for what a unit does in combat, used the same way by the player and by the AI.
/// It validates a request, runs one action at a time (a new request replaces the current one), lets it be
/// cancelled, and writes a short journal to the console (requested / accepted / refused / ended).
/// It only drives UnitMover through the actions; nobody else needs to.
/// Besides moving and waiting, a unit can attack another one (melee for now).
/// </summary>
public class ActionController : MonoBehaviour
{
    [SerializeField] private UnitMover mover;
    [Tooltip("Optional: dead units refuse every action.")]
    [SerializeField] private Health health;
    [Tooltip("Optional: authorizes and limits the moves (combat manager, campaign travel). Empty = free moves.")]
    [SerializeField] private MoveOrderGate gate;
    [Header("Fighting (optional)")]
    [SerializeField] private UnitStats stats;
    [SerializeField] private UnitTeam team;
    [Tooltip("The scene's combat rules. Without them the unit cannot attack.")]
    [SerializeField] private CombatRulesProvider rulesProvider;
    [SerializeField] private bool logActions = true;

    private MoveAction moveAction;
    private WaitAction waitAction;
    private AttackAction attackAction;
    private UnitAction current;
    private Transform focus;
    private Health focusHealth;

    /// <summary>The action running now, or null.</summary>
    public UnitAction Current => current;
    public bool IsBusy => current != null;

    /// <summary>
    /// The enemy this unit has its eyes on: the one it was told to attack, or else the first one that attacked it.
    /// It keeps it until that enemy is gone or the unit is given another order. Null when it has none.
    /// </summary>
    public Transform Focus => IsFocusValid ? focus : null;

    private bool IsFocusValid => focus != null && focusHealth != null && !focusHealth.IsDead;

    /// <summary>Raised when a request was accepted and the action starts.</summary>
    public event Action<UnitAction> ActionStarted = delegate { };
    /// <summary>Raised when an action ends, with how it ended. The controller is already free when this is raised.</summary>
    public event Action<UnitAction, ActionResult> ActionFinished = delegate { };
    /// <summary>Raised when a request was refused, with the reason.</summary>
    public event Action<UnitAction, string> ActionRefused = delegate { };

    private void Awake()
    {
        if (mover == null)
        {
            Debug.LogError("ActionController: mover is not assigned.", this);
            enabled = false;
            return;
        }

        moveAction = new MoveAction(mover, gate);
        waitAction = new WaitAction();
        if (stats != null && team != null)
        {
            attackAction = new AttackAction(stats, team, mover, moveAction, rulesProvider);
        }
    }

    private void Update()
    {
        if (current != null)
        {
            current.Tick(Time.deltaTime);
        }
    }

    /// <summary>Asks the unit to walk to a point.</summary>
    public bool RequestMove(Vector3 destination)
    {
        return Request(moveAction, new ActionTarget(destination), out _);
    }

    /// <summary>Asks the unit to walk to a point. When refused, 'reason' says why.</summary>
    public bool RequestMove(Vector3 destination, out string reason)
    {
        return Request(moveAction, new ActionTarget(destination), out reason);
    }

    /// <summary>Asks the unit to stand still for some seconds of action time.</summary>
    public bool RequestWait(float seconds)
    {
        waitAction.Duration = seconds;
        return Request(waitAction, new ActionTarget(transform.position), out _);
    }

    /// <summary>Asks the unit to fight an enemy unit until it dies. When refused, 'reason' says why.</summary>
    public bool RequestAttack(Transform target, out string reason)
    {
        if (attackAction == null)
        {
            reason = "cette unité n'a pas de statistiques de combat";
            Debug.Log("[Action] " + name + " · Attaquer refusée : " + reason, this);
            return false;
        }

        return Request(attackAction, new ActionTarget(target != null ? target.position : transform.position, target), out reason);
    }

    /// <summary>Asks the unit to fight an enemy unit until it dies.</summary>
    public bool RequestAttack(Transform target)
    {
        return RequestAttack(target, out _);
    }

    /// <summary>True when the unit is currently fighting this target.</summary>
    public bool IsAttacking(Transform target)
    {
        return attackAction != null && current == attackAction && attackAction.Target == target;
    }

    /// <summary>
    /// Asks for any action. Refused if the unit is dead or the action cannot be done. Otherwise it replaces the
    /// action in progress: the same action asked again just adapts to the new target, another one cancels the old.
    /// </summary>
    public bool Request(UnitAction action, ActionTarget target, out string reason)
    {
        if (health != null && health.IsDead)
        {
            return Refuse(action, "l'unité est morte", out reason);
        }

        if (!action.Validate(target, out string why))
        {
            return Refuse(action, why, out reason);
        }

        reason = null;
        if (current == action)
        {
            // Same action, new target (an AI refreshing its chase, a player clicking again): no journal, no restart.
            action.Retarget(target);
            if (action == attackAction)
            {
                SetFocus(target.Target);
            }

            return true;
        }

        Cancel();

        // An attack gives the unit a focus; any other order makes it let go of it.
        if (action == attackAction)
        {
            SetFocus(target.Target);
        }
        else
        {
            SetFocus(null);
        }

        current = action;
        action.Finished += OnActionFinished;
        Log(action.Name + " acceptée (coût estimé " + action.Cost.ToString("F1") + " s)");
        ActionStarted(action);
        action.Begin(target);
        return true;
    }

    /// <summary>
    /// Tells the unit that 'attacker' has just struck it. A unit with no focus turns toward the attacker and keeps
    /// it as its focus; a unit that already has one stays turned to it (the first target is kept, a notion of
    /// target choice or aggro will come later). A unit that is walking keeps facing where it goes.
    /// </summary>
    public void NoteAttackedBy(Transform attacker)
    {
        if (IsFocusValid && focus != attacker)
        {
            return;
        }

        SetFocus(attacker);
        mover.FaceTowards(attacker.position);
    }

    /// <summary>Cancels the action in progress, if any. The unit stops.</summary>
    public void Cancel()
    {
        if (current != null)
        {
            current.Cancel();
        }
    }

    private void SetFocus(Transform target)
    {
        focus = target;
        focusHealth = target != null ? target.GetComponentInParent<Health>() : null;
    }

    private bool Refuse(UnitAction action, string why, out string reason)
    {
        reason = why;
        Log(action.Name + " refusée : " + why);
        ActionRefused(action, why);
        return false;
    }

    private void OnActionFinished(UnitAction action, ActionResult result)
    {
        action.Finished -= OnActionFinished;
        if (current == action)
        {
            current = null;
        }

        Log(action.Name + " terminée : " + result);
        ActionFinished(action, result);
    }

    private void Log(string message)
    {
        if (logActions)
        {
            Debug.Log("[Action] " + name + " · " + message, this);
        }
    }
}

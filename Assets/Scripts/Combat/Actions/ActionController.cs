using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// The single entry point for what a unit does in combat, used the same way by the player and by the AI.
/// It validates a request, runs one action at a time, lets it be cancelled, and writes a short journal to the
/// console (requested / accepted / refused / ended).
/// A plain request replaces the action in progress and empties the queue; an enqueued order waits behind it
/// (within combat, up to the limit in the combat settings) and starts when the unit is free and has recovered.
/// It only drives UnitMover through the actions; nobody else needs to.
/// Besides moving and waiting, a unit can attack another one: in melee, or from a distance if its weapon has a range.
/// </summary>
public class ActionController : MonoBehaviour
{
    private struct QueuedOrder
    {
        public ActionKind Kind;
        public Vector3 Point;
        public Transform Target;
        public float Seconds;
    }

    [SerializeField] private UnitMover mover;
    [Tooltip("Optional: dead units refuse every action.")]
    [SerializeField] private Health health;
    [Tooltip("Optional: authorizes and limits the moves (campaign travel). Empty = free moves.")]
    [SerializeField] private MoveOrderGate gate;
    [Header("Fighting (optional)")]
    [SerializeField] private UnitStats stats;
    [SerializeField] private UnitTeam team;
    [Tooltip("The scene's combat rules. Without them the unit cannot attack.")]
    [SerializeField] private CombatRulesProvider rulesProvider;
    [Tooltip("The scene's combat manager: gives the length of the order queue and the recovery after actions. Without it there is no queue.")]
    [SerializeField] private CombatManager combat;
    [SerializeField] private bool logActions = true;

    private readonly List<QueuedOrder> queue = new List<QueuedOrder>();
    private readonly StringBuilder summary = new StringBuilder();
    private MoveAction moveAction;
    private WaitAction waitAction;
    private AttackAction attackAction;
    private ShootAction shootAction;
    private UnitAction current;
    private float recoveryRemaining;
    private Transform focus;
    private Health focusHealth;

    /// <summary>The action running now, or null.</summary>
    public UnitAction Current => current;
    public bool IsBusy => current != null;

    /// <summary>How many orders wait behind the current action.</summary>
    public int QueuedCount => queue.Count;

    /// <summary>True during the short recovery after an action, when the next queued order is not started yet.</summary>
    public bool IsRecovering => recoveryRemaining > 0f;

    /// <summary>
    /// The enemy this unit has its eyes on: the one it was told to attack, or else the first one that attacked it.
    /// It keeps it until that enemy is gone or the unit is given another order. Null when it has none.
    /// </summary>
    public Transform Focus => IsFocusValid ? focus : null;

    private bool IsFocusValid => focus != null && focusHealth != null && !focusHealth.IsDead;

    // The attack this unit makes: shooting for a ranged weapon, striking otherwise.
    private UnitAction AttackFor => stats != null && stats.IsRanged ? shootAction : attackAction;

    private int MaxQueuedOrders => combat != null && combat.Settings != null ? combat.Settings.MaxQueuedOrders : 0;

    /// <summary>Raised when a request was accepted and the action starts.</summary>
    public event Action<UnitAction> ActionStarted = delegate { };
    /// <summary>Raised when an action ends, with how it ended. The controller is already free when this is raised.</summary>
    public event Action<UnitAction, ActionResult> ActionFinished = delegate { };
    /// <summary>Raised when a request was refused, with the reason.</summary>
    public event Action<UnitAction, string> ActionRefused = delegate { };

    /// <summary>
    /// Gives a unit made by a script (a combat spawner) the scene's rules and combat manager, which a prefab cannot
    /// hold. Only before the unit wakes up: the actions are built with them at that moment.
    /// </summary>
    public void BindScene(CombatRulesProvider sceneRules, CombatManager sceneCombat)
    {
        if (moveAction != null)
        {
            Debug.LogError("ActionController: BindScene must be called before the unit wakes up.", this);
            return;
        }

        rulesProvider = sceneRules;
        combat = sceneCombat;
    }

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
            attackAction = new AttackAction(stats, team, mover, moveAction, rulesProvider,
                combat != null ? combat.Timing : null);
            shootAction = new ShootAction(stats, team, mover, moveAction, rulesProvider, combat);
        }
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        if (current != null)
        {
            current.Tick(deltaTime);
        }

        if (recoveryRemaining > 0f)
        {
            recoveryRemaining -= deltaTime;
        }

        if (current == null && queue.Count > 0 && recoveryRemaining <= 0f)
        {
            StartNext();
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

    /// <summary>Asks the unit to stand still for some seconds.</summary>
    public bool RequestWait(float seconds)
    {
        waitAction.Duration = seconds;
        return Request(waitAction, new ActionTarget(transform.position), out _);
    }

    /// <summary>Asks the unit to fight an enemy unit until it dies. When refused, 'reason' says why.</summary>
    public bool RequestAttack(Transform target, out string reason)
    {
        UnitAction attack = AttackFor;
        if (attack == null)
        {
            reason = "cette unité n'a pas de statistiques de combat";
            Debug.Log("[Action] " + name + " · Attaquer refusée : " + reason, this);
            return false;
        }

        return Request(attack, new ActionTarget(target != null ? target.position : transform.position, target), out reason);
    }

    /// <summary>Asks the unit to fight an enemy unit until it dies.</summary>
    public bool RequestAttack(Transform target)
    {
        return RequestAttack(target, out _);
    }

    /// <summary>Lines up a walk behind the current order (starts it at once if the unit is free).</summary>
    public bool EnqueueMove(Vector3 destination)
    {
        return Enqueue(new QueuedOrder { Kind = ActionKind.Move, Point = destination });
    }

    /// <summary>Lines up a wait behind the current order.</summary>
    public bool EnqueueWait(float seconds)
    {
        return Enqueue(new QueuedOrder { Kind = ActionKind.Wait, Seconds = seconds });
    }

    /// <summary>Lines up an attack behind the current order.</summary>
    public bool EnqueueAttack(Transform target)
    {
        return Enqueue(new QueuedOrder { Kind = ActionKind.Attack, Target = target });
    }

    /// <summary>True when the unit is currently fighting this target.</summary>
    public bool IsAttacking(Transform target)
    {
        return current != null && current.Kind == ActionKind.Attack && current.Target == target;
    }

    /// <summary>
    /// Asks for any action and drops the queue. Refused if the unit is dead or the action cannot be done.
    /// Otherwise it replaces the action in progress: the same action asked again just adapts to the new target,
    /// another one cancels the old.
    /// </summary>
    public bool Request(UnitAction action, ActionTarget target, out string reason)
    {
        queue.Clear();
        recoveryRemaining = 0f;
        return RunAction(action, target, out reason);
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

    /// <summary>Cancels the action in progress and drops the queue. The unit stops.</summary>
    public void Cancel()
    {
        queue.Clear();
        recoveryRemaining = 0f;
        CancelCurrent();
    }

    /// <summary>
    /// Where the unit will be once it has carried out everything it was told (the current action and the queue):
    /// the end of the last walk, or the position of the last target to attack. A wait changes nothing.
    /// False when it has nothing lined up, or outside combat where there is no queue.
    /// </summary>
    public bool TryGetLastOrderPoint(out Vector3 point)
    {
        point = default;
        if (MaxQueuedOrders <= 0)
        {
            return false;
        }

        bool found = false;
        if (current is MoveAction move)
        {
            point = move.Destination;
            found = true;
        }
        else if (current != null && current.Kind == ActionKind.Attack && current.Target != null)
        {
            point = current.Target.position;
            found = true;
        }

        for (int i = 0; i < queue.Count; i++)
        {
            if (queue[i].Kind == ActionKind.Move)
            {
                point = queue[i].Point;
                found = true;
            }
            else if (queue[i].Kind == ActionKind.Attack && queue[i].Target != null)
            {
                point = queue[i].Target.position;
                found = true;
            }
        }

        return found;
    }

    /// <summary>
    /// Where the order being carried out ends: the destination of the current walk, or the position of the target
    /// being attacked. False when the unit is not walking or attacking.
    /// </summary>
    public bool TryGetCurrentOrderPoint(out Vector3 point)
    {
        point = default;
        if (current is MoveAction move)
        {
            point = move.Destination;
            return true;
        }

        if (current != null && current.Kind == ActionKind.Attack && current.Target != null)
        {
            point = current.Target.position;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Copies, in order, the points the queued orders lead to (the destination of each queued walk, the position of
    /// each target to attack; waits lead nowhere). Returns how many were written. No allocation.
    /// </summary>
    public int CopyQueuedPoints(Vector3[] buffer)
    {
        int count = 0;
        for (int i = 0; i < queue.Count && count < buffer.Length; i++)
        {
            if (queue[i].Kind == ActionKind.Move)
            {
                buffer[count++] = queue[i].Point;
            }
            else if (queue[i].Kind == ActionKind.Attack && queue[i].Target != null)
            {
                buffer[count++] = queue[i].Target.position;
            }
        }

        return count;
    }

    /// <summary>
    /// Fills the list with one short label per order: the current action (or the recovery) first, then the queued
    /// orders in turn. Returns how many there are. For the interface.
    /// </summary>
    public int GetOrderLabels(List<string> labels)
    {
        labels.Clear();
        if (current != null)
        {
            labels.Add(current.Kind == ActionKind.Attack && current.Target != null
                ? current.Name + " " + current.Target.name
                : current.Name);
        }
        else if (recoveryRemaining > 0f)
        {
            labels.Add("Récupération");
        }

        for (int i = 0; i < queue.Count; i++)
        {
            labels.Add(queue[i].Kind == ActionKind.Attack && queue[i].Target != null
                ? NameOf(queue[i].Kind) + " " + queue[i].Target.name
                : NameOf(queue[i].Kind));
        }

        return labels.Count;
    }

    /// <summary>The current action and the queued orders, in one line, for the interface.</summary>
    public string DescribeOrders()
    {
        summary.Clear();
        if (current != null && current.Kind == ActionKind.Attack && current.Target != null)
        {
            summary.Append(current.Name).Append(' ').Append(current.Target.name);
        }
        else if (current != null)
        {
            summary.Append(current.Name);
        }
        else
        {
            summary.Append(recoveryRemaining > 0f ? "Récupération" : "Inactif");
        }

        for (int i = 0; i < queue.Count; i++)
        {
            summary.Append("  >  ").Append(NameOf(queue[i].Kind));
            if (queue[i].Kind == ActionKind.Attack && queue[i].Target != null)
            {
                summary.Append(' ').Append(queue[i].Target.name);
            }
        }

        return summary.ToString();
    }

    private bool Enqueue(QueuedOrder order)
    {
        if (health != null && health.IsDead)
        {
            Log(NameOf(order.Kind) + " refusée : l'unité est morte");
            return false;
        }

        int max = MaxQueuedOrders;
        if (max <= 0)
        {
            // No queue outside combat: a plain order.
            return StartOrder(order);
        }

        if (current == null && queue.Count == 0 && recoveryRemaining <= 0f)
        {
            return StartOrder(order);
        }

        if (queue.Count >= max)
        {
            Log(NameOf(order.Kind) + " refusée : la file d'ordres est pleine (" + max + ")");
            return false;
        }

        queue.Add(order);
        Log(NameOf(order.Kind) + " mise en file (rang " + queue.Count + ")");
        return true;
    }

    // Starts the next queued order that can still be done; the ones that cannot (a dead target...) are skipped.
    private void StartNext()
    {
        while (queue.Count > 0)
        {
            QueuedOrder order = queue[0];
            queue.RemoveAt(0);
            if (StartOrder(order))
            {
                return;
            }
        }
    }

    private bool StartOrder(QueuedOrder order)
    {
        string reason;
        switch (order.Kind)
        {
            case ActionKind.Move:
                return RunAction(moveAction, new ActionTarget(order.Point), out reason);

            case ActionKind.Wait:
                waitAction.Duration = order.Seconds;
                return RunAction(waitAction, new ActionTarget(transform.position), out reason);

            default:
                UnitAction attack = AttackFor;
                if (attack == null)
                {
                    Log("Attaquer refusée : cette unité n'a pas de statistiques de combat");
                    return false;
                }

                return RunAction(attack,
                    new ActionTarget(order.Target != null ? order.Target.position : transform.position, order.Target),
                    out reason);
        }
    }

    // Validates and starts an action, replacing the one in progress. The queue is left alone.
    private bool RunAction(UnitAction action, ActionTarget target, out string reason)
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
            if (action.Kind == ActionKind.Attack)
            {
                SetFocus(target.Target);
            }

            return true;
        }

        CancelCurrent();

        // An attack gives the unit a focus; any other order makes it let go of it.
        if (action.Kind == ActionKind.Attack)
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

    private void CancelCurrent()
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
        if (team != null)
        {
            CombatEvents.RaiseOrderRefused(team.Team, name, why);
        }

        return false;
    }

    private void OnActionFinished(UnitAction action, ActionResult result)
    {
        action.Finished -= OnActionFinished;
        if (current == action)
        {
            current = null;
        }

        // A short recovery before the next thing, unless somebody stopped the action on purpose.
        if (result != ActionResult.Cancelled && combat != null && combat.Timing != null)
        {
            recoveryRemaining = combat.Timing.RecoveryAfter(action.Kind);
        }

        Log(action.Name + " terminée : " + result);
        ActionFinished(action, result);
    }

    private static string NameOf(ActionKind kind)
    {
        switch (kind)
        {
            case ActionKind.Move:
                return "Déplacer";
            case ActionKind.Wait:
                return "Attendre";
            default:
                return "Attaquer";
        }
    }

    private void Log(string message)
    {
        if (logActions)
        {
            Debug.Log("[Action] " + name + " · " + message, this);
        }
    }
}

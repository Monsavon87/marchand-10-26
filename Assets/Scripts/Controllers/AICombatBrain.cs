using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// The combat decisions of an AI unit. While a combat is running and the unit knows of an enemy (one it sees, or
/// one that attacked it), it takes over from AIUnitController (patrol, chase, search) and decides every fraction
/// of a second: who to fight, whether to walk around to the enemy's flank, whether a ranged unit should back
/// away, whether to fall back when hurt. When no enemy is known for a while it hands control back.
/// It acts only through the unit's ActionController, exactly like the player's orders, and knows only what the
/// unit can see (no hidden information). How it behaves comes from its AIProfile.
/// </summary>
public class AICombatBrain : MonoBehaviour
{
    private const int MaxKnown = 24;
    private const int RetreatAngles = 16;
    private const int FlankAngles = 12;
    private const float SnapRadius = 1.5f;
    // Meters of walk a unit accepts to avoid one unit of risk when it picks a spot.
    private const float RiskWeight = 6f;
    // Enemies farther than this (m) are ignored when judging how exposed a position is.
    private const float ExposureRange = 12f;
    private const float FlankMaxDetourFactor = 1.5f;
    private const float FlankMaxSeconds = 8f;
    private const float BlockedSeconds = 3f;
    private const float RetreatMinGain = 2f;
    private const float RetreatPanicDistance = 5f;
    private static readonly float[] RetreatRadii = { 5f, 8f, 11f };

    [SerializeField] private AIUnitController controller;
    [SerializeField] private ActionController actions;
    [SerializeField] private UnitStats stats;
    [SerializeField] private UnitTeam team;
    [SerializeField] private Health health;
    [SerializeField] private UnitMover mover;
    [SerializeField] private AIProfile profile;
    [SerializeField] private CombatManager combat;
    [SerializeField] private CombatRulesProvider rulesProvider;
    [Tooltip("The unit notices enemies closer than this (m) if nothing solid is in the way. A ranged unit sees at least as far as its weapon reaches.")]
    [Min(0f)] [SerializeField] private float visionRadius = 9f;
    [Tooltip("Seconds without any known enemy before the unit goes back to its patrol.")]
    [Min(0f)] [SerializeField] private float loseEnemyTime = 5f;
    [Tooltip("Writes each decision to the console (target and reason).")]
    [SerializeField] private bool showDecisions;

    private readonly List<UnitTeam> known = new List<UnitTeam>(MaxKnown);
    private readonly Dictionary<Transform, float> blockedUntil = new Dictionary<Transform, float>();
    private System.Func<Vector3, float> riskFunction;
    private bool active;
    private float nextThink;
    private float lastEnemySeen;
    private Transform currentTarget;
    private float lastSwitchTime;
    private bool retreating;
    private int retreatMoves;
    private int kiteMoves;
    private bool kiting;
    private bool flanking;
    private float flankStart;
    private string lastDecision;

    /// <summary>True while the brain, not AIUnitController, drives the unit.</summary>
    public bool IsActive => active;

    /// <summary>The last decision taken, written in French (empty until the unit decides something).</summary>
    public string LastDecision => lastDecision;

    private void Start()
    {
        if (controller == null || actions == null || stats == null || team == null || health == null || mover == null
            || profile == null || combat == null || rulesProvider == null)
        {
            Debug.LogError("AICombatBrain: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        riskFunction = PositionRisk;
    }

    private void OnEnable()
    {
        if (actions != null)
        {
            actions.ActionFinished += OnActionFinished;
        }
    }

    private void OnDisable()
    {
        if (actions != null)
        {
            actions.ActionFinished -= OnActionFinished;
        }
    }

    private void Update()
    {
        float now = Time.time;
        if (now < nextThink)
        {
            return;
        }

        nextThink = now + profile.DecisionInterval;
        Think(now);
    }

    private void Think(float now)
    {
        if (combat.State != CombatState.Running || health.IsDead)
        {
            if (active)
            {
                Deactivate();
            }

            return;
        }

        FindKnownEnemies();
        if (known.Count == 0)
        {
            if (active && now - lastEnemySeen > loseEnemyTime)
            {
                Deactivate();
            }

            return;
        }

        lastEnemySeen = now;
        if (!active)
        {
            Activate();
        }

        float healthFraction = health.Max > 0 ? (float)health.Current / health.Max : 1f;
        if (retreating)
        {
            if (healthFraction >= profile.ReturnAboveHealth)
            {
                retreating = false;
                retreatMoves = 0;
                Say("reprend le combat (PV " + health.Current + "/" + health.Max + ")");
            }
            else
            {
                ContinueRetreat();
                return;
            }
        }

        if (profile.RetreatBelowHealth > 0f && healthFraction <= profile.RetreatBelowHealth
            && retreatMoves < profile.MaxRetreatMoves)
        {
            if (TryRetreat())
            {
                return;
            }

            Say("ne peut pas fuir, se bat");
        }

        Transform target = ChooseTarget(now);
        if (target == null)
        {
            Say("aucune cible possible pour l'instant");
            return;
        }

        if (stats.IsRanged)
        {
            FightRanged(target);
        }
        else
        {
            FightMelee(target, now);
        }
    }

    private void Activate()
    {
        active = true;
        currentTarget = null;
        retreating = false;
        retreatMoves = 0;
        kiteMoves = 0;
        flanking = false;
        controller.Interrupt();
        Say("entre en combat (profil " + profile.DisplayName + ")");
    }

    private void Deactivate()
    {
        active = false;
        currentTarget = null;
        retreating = false;
        flanking = false;
        kiting = false;
        controller.TakeBackControl();
        Say("plus d'ennemi en vue, reprend son service");
    }

    // The enemies this unit knows: those it sees within its vision, and the one that attacked it.
    private void FindKnownEnemies()
    {
        known.Clear();
        CombatSettings settings = combat.Settings;
        Vector3 eye = Vector3.up * settings.EyeHeight;
        Vector3 myPosition = mover.transform.position;
        float radius = Mathf.Max(visionRadius, stats.MaxRange);
        IReadOnlyList<UnitTeam> all = UnitTeam.All;
        for (int i = 0; i < all.Count && known.Count < MaxKnown; i++)
        {
            UnitTeam other = all[i];
            if (other.Team == team.Team || !IsAlive(other.transform))
            {
                continue;
            }

            Vector3 otherPosition = other.transform.position;
            if (FlatDistance(myPosition, otherPosition) <= radius
                && LineOfSight.IsClear(myPosition + eye, otherPosition + eye, settings.SightBlockers))
            {
                known.Add(other);
            }
        }

        // Whoever attacked this unit is known even out of sight (an arrow from afar).
        Transform focus = actions.Focus;
        if (focus != null && IsAlive(focus))
        {
            UnitTeam focusTeam = focus.GetComponentInParent<UnitTeam>();
            if (focusTeam != null && focusTeam.Team != team.Team && !known.Contains(focusTeam) && known.Count < MaxKnown)
            {
                known.Add(focusTeam);
            }
        }
    }

    private static bool IsAlive(Transform unit)
    {
        Health unitHealth = unit.GetComponentInParent<Health>();
        return unitHealth != null && !unitHealth.IsDead;
    }

    // ---------------------------------------------------------------- targeting

    private Transform ChooseTarget(float now)
    {
        Vector3 myPosition = mover.transform.position;
        float maxDistance = 0.01f;
        float maxHealth = 0.01f;
        float maxDanger = 0.01f;
        for (int i = 0; i < known.Count; i++)
        {
            if (IsBlocked(known[i].transform, now))
            {
                continue;
            }

            maxDistance = Mathf.Max(maxDistance, FlatDistance(myPosition, known[i].transform.position));
            UnitStats otherStats = known[i].GetComponent<UnitStats>();
            Health otherHealth = known[i].GetComponent<Health>();
            maxHealth = Mathf.Max(maxHealth, otherHealth.Current);
            maxDanger = Mathf.Max(maxDanger, Danger(otherStats));
        }

        Transform best = null;
        float bestScore = float.MinValue;
        float currentScore = float.MinValue;
        string bestWhy = null;
        bool currentStillValid = currentTarget != null && IsAlive(currentTarget) && !IsBlocked(currentTarget, now);
        for (int i = 0; i < known.Count; i++)
        {
            Transform candidate = known[i].transform;
            if (IsBlocked(candidate, now))
            {
                continue;
            }

            float score = ScoreTarget(known[i], myPosition, maxDistance, maxHealth, maxDanger, out string why);
            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
                bestWhy = why;
            }

            if (candidate == currentTarget)
            {
                currentScore = score;
            }
        }

        if (best == null)
        {
            return currentStillValid ? currentTarget : null;
        }

        if (currentStillValid)
        {
            // Keep the target unless another one is clearly better, and not too often.
            bool clearlyBetter = currentScore == float.MinValue
                || (best != currentTarget && bestScore > currentScore + profile.SwitchTargetMargin
                    && now - lastSwitchTime >= profile.SwitchTargetDelay);
            if (!clearlyBetter)
            {
                return currentTarget;
            }
        }

        if (best != currentTarget)
        {
            currentTarget = best;
            lastSwitchTime = now;
            flanking = false;
            kiting = false;
            kiteMoves = 0;
            Say("nouvelle cible " + best.name + " (" + bestWhy + ")");
        }

        return currentTarget;
    }

    private float ScoreTarget(UnitTeam candidate, Vector3 myPosition, float maxDistance, float maxHealth, float maxDanger,
        out string why)
    {
        float distance = FlatDistance(myPosition, candidate.transform.position);
        Health otherHealth = candidate.GetComponent<Health>();
        UnitStats otherStats = candidate.GetComponent<UnitStats>();

        float rule;
        string ruleText;
        switch (profile.TargetRule)
        {
            case AITargetRule.Weakest:
                rule = 1f - otherHealth.Current / maxHealth;
                ruleText = "la plus faible, " + otherHealth.Current + " PV";
                break;
            case AITargetRule.MostDangerous:
                rule = Danger(otherStats) / maxDanger;
                ruleText = "la plus dangereuse";
                break;
            default:
                rule = 1f - distance / maxDistance;
                ruleText = "la plus proche, " + distance.ToString("F1") + " m";
                break;
        }

        float weakPoint = ZoneValue(rulesProvider.Rules.DetermineZone(myPosition, candidate.transform));
        float protect = ThreatensAlly(candidate) ? 1f : 0f;
        float score = rule + profile.WeakPointWeight * weakPoint + profile.ProtectAlliesWeight * protect;

        why = showDecisions
            ? ruleText + (weakPoint > 0f ? ", point faible exposé" : string.Empty)
                + (protect > 0f ? ", menace un allié" : string.Empty) + ", score " + score.ToString("F2")
            : null;
        return score;
    }

    private static float Danger(UnitStats unit)
    {
        return unit.WeaponDamage / Mathf.Max(0.1f, unit.AttackTime);
    }

    // True when this enemy has one of this unit's allies as its focus.
    private bool ThreatensAlly(UnitTeam enemy)
    {
        ActionController enemyActions = enemy.GetComponent<ActionController>();
        Transform focus = enemyActions != null ? enemyActions.Focus : null;
        if (focus == null)
        {
            return false;
        }

        UnitTeam focusTeam = focus.GetComponentInParent<UnitTeam>();
        return focusTeam != null && focusTeam.Team == team.Team && focusTeam.transform != mover.transform;
    }

    private static float ZoneValue(AttackZone zone)
    {
        switch (zone)
        {
            case AttackZone.Back:
                return 1f;
            case AttackZone.Flank:
                return 0.5f;
            default:
                return 0f;
        }
    }

    private bool IsBlocked(Transform unit, float now)
    {
        return blockedUntil.TryGetValue(unit, out float until) && now < until;
    }

    // An attack that turned out to be impossible (no way to reach or shoot the target): leave that target alone a while.
    private void OnActionFinished(UnitAction action, ActionResult result)
    {
        if (active && result == ActionResult.Impossible && action.Kind == ActionKind.Attack && action.Target != null)
        {
            blockedUntil[action.Target] = Time.time + BlockedSeconds;
            Say("ne peut pas atteindre " + action.Target.name + ", change de cible");
            if (currentTarget == action.Target)
            {
                currentTarget = null;
            }
        }
    }

    // ---------------------------------------------------------------- fighting

    private void FightMelee(Transform target, float now)
    {
        Vector3 myPosition = mover.transform.position;
        float distance = FlatDistance(myPosition, target.position);

        if (flanking)
        {
            // Walking around to the flank: let it finish, then strike.
            if (actions.IsBusy && now - flankStart < FlankMaxSeconds)
            {
                return;
            }

            flanking = false;
        }
        else if (profile.FlankingManeuver && profile.WeakPointWeight > 0f && !actions.IsAttacking(target)
                 && distance > stats.Reach * 2f && TryFlank(target, myPosition, distance))
        {
            flanking = true;
            flankStart = now;
            return;
        }

        Attack(target);
    }

    private void FightRanged(Transform target)
    {
        if (kiting)
        {
            // Still backing away: let it finish before deciding anything else.
            if (actions.IsBusy)
            {
                return;
            }

            kiting = false;
        }

        Vector3 myPosition = mover.transform.position;
        float comfort = stats.MaxRange * profile.ComfortDistanceFraction;
        float nearest = NearestEnemyDistance(myPosition);

        if (nearest >= comfort * 1.3f)
        {
            kiteMoves = 0;
        }
        else if (nearest < comfort && kiteMoves < profile.MaxKiteMoves)
        {
            if (ShotSpotFinder.TryFind(stats, mover, combat.Settings, team.Team, myPosition, target, comfort, riskFunction,
                    RiskWeight, out Vector3 spot) && actions.RequestMove(spot))
            {
                kiteMoves++;
                kiting = true;
                Say("recule pour garder ses distances (ennemi à " + nearest.ToString("F1") + " m)");
                return;
            }
        }

        Attack(target);
    }

    private void Attack(Transform target)
    {
        if (actions.IsAttacking(target))
        {
            return;
        }

        if (!actions.RequestAttack(target, out string reason))
        {
            blockedUntil[target] = Time.time + BlockedSeconds;
            Say("attaque de " + target.name + " refusée : " + reason);
            if (currentTarget == target)
            {
                currentTarget = null;
            }

            return;
        }

        Say((stats.IsRanged ? "tire sur " : "attaque ") + target.name);
    }

    // Looks for a place on the flank or back of the target that is not too far out of the way.
    private bool TryFlank(Transform target, Vector3 myPosition, float directDistance)
    {
        float currentValue = ZoneValue(rulesProvider.Rules.DetermineZone(myPosition, target));
        float radius = Mathf.Max(0.5f, stats.Reach * 0.85f);
        bool found = false;
        float bestScore = float.MinValue;
        Vector3 best = default;
        for (int i = 0; i < FlankAngles; i++)
        {
            float angle = i * Mathf.PI * 2f / FlankAngles;
            Vector3 candidate = target.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, SnapRadius, NavMesh.AllAreas))
            {
                continue;
            }

            float value = ZoneValue(rulesProvider.Rules.DetermineZone(hit.position, target));
            if (value <= currentValue || !mover.TryGetPath(hit.position, out float length, out _)
                || length > directDistance * FlankMaxDetourFactor + 2f)
            {
                continue;
            }

            float score = value * 10f - length * 0.3f - PositionRisk(hit.position) * RiskWeight * 0.3f;
            if (score > bestScore)
            {
                found = true;
                bestScore = score;
                best = hit.position;
            }
        }

        if (!found || !actions.RequestMove(best))
        {
            return false;
        }

        Say("contourne " + target.name + " pour l'attaquer par son " + (ZoneValue(rulesProvider.Rules.DetermineZone(best, target)) > 0.9f ? "dos" : "flanc"));
        return true;
    }

    // ---------------------------------------------------------------- retreat

    private bool TryRetreat()
    {
        CombatSettings settings = combat.Settings;
        Vector3 eye = Vector3.up * settings.EyeHeight;
        Vector3 myPosition = mover.transform.position;
        float currentSafety = NearestEnemyDistance(myPosition);

        bool found = false;
        float bestScore = float.MinValue;
        Vector3 best = default;
        for (int ring = 0; ring < RetreatRadii.Length; ring++)
        {
            for (int i = 0; i < RetreatAngles; i++)
            {
                float angle = i * Mathf.PI * 2f / RetreatAngles;
                Vector3 candidate = myPosition + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * RetreatRadii[ring];
                if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, SnapRadius, NavMesh.AllAreas))
                {
                    continue;
                }

                float safety = NearestEnemyDistance(hit.position);
                if (safety < currentSafety + RetreatMinGain || !mover.TryGetPath(hit.position, out float length, out _))
                {
                    continue;
                }

                bool hidden = true;
                for (int e = 0; e < known.Count && hidden; e++)
                {
                    Vector3 enemyPosition = known[e].transform.position;
                    hidden = !LineOfSight.IsClear(hit.position + eye, enemyPosition + eye, settings.SightBlockers);
                }

                float score = safety + (hidden ? 4f : 0f) - length * 0.2f - PositionRisk(hit.position) * RiskWeight * 0.3f;
                if (score > bestScore)
                {
                    found = true;
                    bestScore = score;
                    best = hit.position;
                }
            }
        }

        if (!found || !actions.RequestMove(best))
        {
            return false;
        }

        retreating = true;
        retreatMoves++;
        flanking = false;
        Say("se replie (PV " + health.Current + "/" + health.Max + "), repli " + retreatMoves + "/" + profile.MaxRetreatMoves);
        return true;
    }

    // At its fall-back spot the unit waits. If an enemy comes close again it moves away again, a few times, then fights.
    private void ContinueRetreat()
    {
        if (actions.IsBusy)
        {
            return;
        }

        if (NearestEnemyDistance(mover.transform.position) > RetreatPanicDistance)
        {
            return;
        }

        if (retreatMoves >= profile.MaxRetreatMoves || !TryRetreat())
        {
            retreating = false;
            Say("rattrapé, se bat");
        }
    }

    // ---------------------------------------------------------------- helpers

    private float NearestEnemyDistance(Vector3 position)
    {
        float nearest = float.MaxValue;
        for (int i = 0; i < known.Count; i++)
        {
            nearest = Mathf.Min(nearest, FlatDistance(position, known[i].transform.position));
        }

        return nearest;
    }

    // How risky it is to stand here: in the flank or back of nearby enemies (if the profile cares), and, for a
    // ranged unit, too close to one.
    private float PositionRisk(Vector3 position)
    {
        float risk = 0f;
        float comfort = stats.MaxRange * profile.ComfortDistanceFraction;
        for (int i = 0; i < known.Count; i++)
        {
            Transform enemy = known[i].transform;
            float distance = FlatDistance(position, enemy.position);
            if (profile.AvoidExposedPositions && distance < ExposureRange)
            {
                risk += ZoneValue(rulesProvider.Rules.DetermineZone(position, enemy));
            }

            if (stats.IsRanged && distance < comfort)
            {
                risk += 2f;
            }
        }

        return risk;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return Mathf.Sqrt(dx * dx + dz * dz);
    }

    // Remembers a decision and, in debug mode, writes it to the console once (not at every tick).
    private void Say(string decision)
    {
        if (decision == lastDecision)
        {
            return;
        }

        lastDecision = decision;
        if (showDecisions)
        {
            Debug.Log("[IA] " + name + " · " + decision, this);
        }
    }
}

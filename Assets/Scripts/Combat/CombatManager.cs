using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Where a combat stands.</summary>
public enum CombatState
{
    NotStarted,
    Running,
    Ended
}

/// <summary>Why the game is paused.</summary>
public enum PauseCause
{
    None,
    /// <summary>The player pressed the pause key.</summary>
    Manual,
    /// <summary>A unit of the player's side died.</summary>
    AllyDied,
    /// <summary>An enemy unit has noticed one of the player's units.</summary>
    EnemyDetected
}

/// <summary>
/// Runs a combat in real time with pause. It starts with the units present, follows who is still alive, ends the
/// combat when a side has nobody left, pauses the game on the player's request (Space) or by itself on the events
/// the settings ask for (an ally died, an enemy unit noticed one of the player's units), and tells the rest of the
/// game through events.
/// It never moves a unit: UnitMover executes, the action system decides what a unit does.
/// There are no rounds; the pace of actions comes from the actions' own durations and recoveries.
/// </summary>
public class CombatManager : MonoBehaviour
{
    private struct Tracked
    {
        public UnitTeam Unit;
        public Health Health;
        public bool Alive;
    }

    [SerializeField] private CombatSettings settings;
    [SerializeField] private ActionTimingSettings timing;
    [SerializeField] private Key pauseKey = Key.Space;
    [Tooltip("Start the combat when the scene starts, with the units that are there.")]
    [SerializeField] private bool startAutomatically = true;

    private Tracked[] players = Array.Empty<Tracked>();
    private Tracked[] enemies = Array.Empty<Tracked>();
    // The enemy units that have already noticed the player: each one pauses the game once, the first time.
    private readonly List<UnitTeam> enemiesThatNoticed = new List<UnitTeam>();

    public CombatState State { get; private set; }
    public bool IsPaused { get; private set; }
    public PauseCause PauseCause { get; private set; }
    /// <summary>The side that won, once the combat has ended with a winner.</summary>
    public Team? Winner { get; private set; }
    public CombatSettings Settings => settings;
    public ActionTimingSettings Timing => timing;
    public int PlayersAlive { get; private set; }
    public int EnemiesAlive { get; private set; }

    /// <summary>Raised when the game is paused (true) or resumed (false).</summary>
    public event Action<bool> PauseChanged = delegate { };
    /// <summary>Raised when a combat starts, once the units are counted.</summary>
    public event Action CombatStarted = delegate { };
    /// <summary>Raised when a side has no unit left: the winner, or null if both sides fell together.</summary>
    public event Action<Team?> CombatEnded = delegate { };

    private void Start()
    {
        if (settings == null)
        {
            Debug.LogError("CombatManager: settings is not assigned.", this);
            enabled = false;
            return;
        }

        if (startAutomatically)
        {
            StartCombat();
        }
    }

    private void OnEnable()
    {
        Time.timeScale = 1f;
        CombatEvents.UnitDetected += OnUnitDetected;
    }

    private void OnDisable()
    {
        CombatEvents.UnitDetected -= OnUnitDetected;
        // Never leave the game frozen.
        Time.timeScale = 1f;
        IsPaused = false;
        PauseCause = PauseCause.None;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard[pauseKey].wasPressedThisFrame)
        {
            if (IsPaused)
            {
                SetPaused(false);
            }
            else
            {
                SetPaused(true, PauseCause.Manual);
            }
        }

        if (State != CombatState.Running)
        {
            return;
        }

        CheckDeaths();
    }

    /// <summary>Starts a combat with the units of both sides that are in the scene now.</summary>
    public void StartCombat()
    {
        var playerList = new System.Collections.Generic.List<Tracked>();
        var enemyList = new System.Collections.Generic.List<Tracked>();
        System.Collections.Generic.IReadOnlyList<UnitTeam> all = UnitTeam.All;
        for (int i = 0; i < all.Count; i++)
        {
            Health health = all[i].GetComponent<Health>();
            if (health == null || health.IsDead)
            {
                continue;
            }

            var tracked = new Tracked { Unit = all[i], Health = health, Alive = true };
            if (all[i].Team == Team.Player)
            {
                playerList.Add(tracked);
            }
            else
            {
                enemyList.Add(tracked);
            }
        }

        if (playerList.Count == 0 || enemyList.Count == 0)
        {
            Debug.LogWarning("CombatManager: a combat needs units on both sides (players " + playerList.Count
                + ", enemies " + enemyList.Count + ").", this);
            return;
        }

        players = playerList.ToArray();
        enemies = enemyList.ToArray();
        enemiesThatNoticed.Clear();
        PlayersAlive = players.Length;
        EnemiesAlive = enemies.Length;
        Winner = null;
        State = CombatState.Running;
        CombatStarted();
    }

    /// <summary>Pauses or resumes the game. 'cause' says why when pausing.</summary>
    public void SetPaused(bool paused, PauseCause cause = PauseCause.Manual)
    {
        if (IsPaused == paused)
        {
            return;
        }

        IsPaused = paused;
        PauseCause = paused ? cause : PauseCause.None;
        Time.timeScale = paused ? 0f : 1f;
        PauseChanged(paused);
    }

    // Notices the units that died since the last frame, pauses if the settings ask for it, and ends the combat
    // when a side is wiped out.
    private void CheckDeaths()
    {
        bool allyDied = false;
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].Alive && IsDown(players[i]))
            {
                players[i].Alive = false;
                PlayersAlive--;
                allyDied = true;
            }
        }

        for (int i = 0; i < enemies.Length; i++)
        {
            if (enemies[i].Alive && IsDown(enemies[i]))
            {
                enemies[i].Alive = false;
                EnemiesAlive--;
            }
        }

        if (allyDied && settings.AutoPauseOnAllyDeath && !IsPaused)
        {
            SetPaused(true, PauseCause.AllyDied);
        }

        if (PlayersAlive <= 0 || EnemiesAlive <= 0)
        {
            Team? winner = null;
            if (PlayersAlive > 0)
            {
                winner = Team.Player;
            }
            else if (EnemiesAlive > 0)
            {
                winner = Team.Enemy;
            }

            Winner = winner;
            State = CombatState.Ended;
            CombatEnded(winner);
        }
    }

    // An enemy unit noticed one of the player's units (its AI says so): the first time for that enemy, the game
    // pauses if the settings ask for it, so the player can react.
    private void OnUnitDetected(UnitTeam detector, UnitTeam detected)
    {
        if (State != CombatState.Running || detector == null || detected == null
            || detector.Team != Team.Enemy || detected.Team != Team.Player || enemiesThatNoticed.Contains(detector))
        {
            return;
        }

        enemiesThatNoticed.Add(detector);
        if (settings.AutoPauseOnEnemyDetection && !IsPaused)
        {
            SetPaused(true, PauseCause.EnemyDetected);
        }
    }

    private static bool IsDown(Tracked tracked)
    {
        return tracked.Unit == null || tracked.Health == null || tracked.Health.IsDead;
    }
}

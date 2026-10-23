using System;
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
    /// <summary>An enemy came into view of one of the player's units.</summary>
    EnemySighted
}

/// <summary>
/// Runs a combat in real time with pause. It starts with the units present, follows who is still alive, ends the
/// combat when a side has nobody left, pauses the game on the player's request (Space) or by itself on the events
/// the settings ask for, and tells the rest of the game through events.
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
    private bool[,] seen = new bool[0, 0];
    private bool sightInitialized;
    private float nextSightCheckTime;

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
    }

    private void OnDisable()
    {
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
        if (State == CombatState.Running && !IsPaused)
        {
            CheckSightings();
        }
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
        seen = new bool[players.Length, enemies.Length];
        sightInitialized = false;
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

    // Who sees whom: a player unit sees an enemy within the view radius with a clear line of sight.
    // The first pass only learns what is already visible; afterwards a new sighting can pause the game.
    private void CheckSightings()
    {
        if (Time.unscaledTime < nextSightCheckTime)
        {
            return;
        }

        nextSightCheckTime = Time.unscaledTime + settings.SightCheckInterval;
        float radiusSquared = settings.ViewRadius * settings.ViewRadius;
        Vector3 eye = Vector3.up * settings.EyeHeight;
        bool newSighting = false;

        for (int p = 0; p < players.Length; p++)
        {
            if (!players[p].Alive)
            {
                continue;
            }

            Vector3 playerPosition = players[p].Unit.transform.position;
            for (int e = 0; e < enemies.Length; e++)
            {
                if (!enemies[e].Alive)
                {
                    seen[p, e] = false;
                    continue;
                }

                Vector3 enemyPosition = enemies[e].Unit.transform.position;
                Vector3 flat = enemyPosition - playerPosition;
                flat.y = 0f;
                bool visible = flat.sqrMagnitude <= radiusSquared
                    && LineOfSight.IsClear(playerPosition + eye, enemyPosition + eye, settings.SightBlockers);

                if (visible && !seen[p, e] && sightInitialized)
                {
                    newSighting = true;
                }

                seen[p, e] = visible;
            }
        }

        sightInitialized = true;
        if (newSighting && settings.AutoPauseOnEnemySighted)
        {
            SetPaused(true, PauseCause.EnemySighted);
        }
    }

    private static bool IsDown(Tracked tracked)
    {
        return tracked.Unit == null || tracked.Health == null || tracked.Health.IsDead;
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;

/// <summary>
/// Decides whether the campaign map's time flows, and is the only one to set the map's Time.timeScale. The world
/// (patrols, calendar, markets later) is frozen while the caravan has no action in progress; it flows while the
/// caravan has one (a move, a chase), or while the player chooses to let time pass with the wait key. The pause key
/// freezes it whatever happens, and a window that needs the map still while it is open (a town, the help page)
/// holds it through SetHeld, independently of the pause and of the other windows: the map goes on only when
/// nothing holds it. Letting time pass ends by itself when an encounter starts, when the caravan reaches a town
/// and on a game over.
/// </summary>
public class CampaignPause : MonoBehaviour
{
    [Tooltip("Keys are physical positions (US layout).")]
    [SerializeField] private Key pauseKey = Key.Space;
    [SerializeField] private Key waitKey = Key.T;
    [Tooltip("Shown while the player has paused the map.")]
    [SerializeField] private GameObject banner;
    [Tooltip("The caravan: its moves are the actions that make the world's time flow.")]
    [SerializeField] private UnitMover caravan;
    [Header("Ends letting time pass")]
    [SerializeField] private CampaignTravel travel;
    [SerializeField] private CampaignEncounters encounters;
    [SerializeField] private CampaignEncounterFlow flow;

    private readonly HashSet<Object> holders = new HashSet<Object>();
    private bool pausedByPlayer;
    private bool waiting;

    public CampaignTimeState State { get; private set; } = CampaignTimeState.Frozen;
    public bool IsPaused => pausedByPlayer || holders.Count > 0;
    /// <summary>True while the player lets time pass.</summary>
    public bool IsWaiting => waiting;

    private void Start()
    {
        if (banner == null || caravan == null || travel == null || encounters == null || flow == null)
        {
            Debug.LogError("CampaignPause: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        Apply();
    }

    private void OnEnable()
    {
        if (travel != null)
        {
            travel.TownReached += OnTownReached;
        }

        if (encounters != null)
        {
            encounters.EncounterStarted += OnEncounterStarted;
        }

        if (flow != null)
        {
            flow.GameOver += StopWaiting;
        }
    }

    private void OnDisable()
    {
        if (travel != null)
        {
            travel.TownReached -= OnTownReached;
        }

        if (encounters != null)
        {
            encounters.EncounterStarted -= OnEncounterStarted;
        }

        if (flow != null)
        {
            flow.GameOver -= StopWaiting;
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard[pauseKey].wasPressedThisFrame)
            {
                pausedByPlayer = !pausedByPlayer;
            }

            // Letting time pass is a choice made on the map itself: not while paused or while a window is open.
            if (keyboard[waitKey].wasPressedThisFrame && !IsPaused)
            {
                waiting = !waiting;
            }
        }

        Apply();
    }

    /// <summary>
    /// Freezes the map while the 'owner' window is open (true), or stops that window's hold (false). Each window
    /// holds the pause on its own: closing one does not unfreeze the map while another is still open.
    /// </summary>
    public void SetHeld(Object owner, bool hold)
    {
        // Nothing changes when the window did not hold the pause (or already does): the time is left alone.
        bool changed = hold ? holders.Add(owner) : holders.Remove(owner);
        if (changed)
        {
            Apply();
        }
    }

    /// <summary>The player stops letting time pass (the world freezes again unless the caravan has an action).</summary>
    public void StopWaiting()
    {
        waiting = false;
    }

    private void OnTownReached(PointOfInterest town)
    {
        StopWaiting();
    }

    private void OnEncounterStarted(UnitTeam band)
    {
        StopWaiting();
    }

    private void Apply()
    {
        CampaignTimeState next;
        if (pausedByPlayer)
        {
            next = CampaignTimeState.Paused;
        }
        else if (holders.Count > 0)
        {
            next = CampaignTimeState.Held;
        }
        else if (caravan.IsMoving || caravan.IsStarting)
        {
            next = CampaignTimeState.Moving;
        }
        else
        {
            next = waiting ? CampaignTimeState.Waiting : CampaignTimeState.Frozen;
        }

        State = next;
        Time.timeScale = next == CampaignTimeState.Moving || next == CampaignTimeState.Waiting ? 1f : 0f;
        banner.SetActive(next == CampaignTimeState.Paused);
    }
}

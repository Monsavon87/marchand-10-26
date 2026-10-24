using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Brings the beaten enemy bands back, so the map never runs out of patrols: the goal is long-term trade, not wiping
/// them out. When the encounter flow reports a band beaten (and switches it off), the delay is drawn between the
/// settings' minimum and maximum, counted in world days only: the campaign clock raises DayStarted, nothing here has a
/// timer. When the day comes, at midnight, the band returns to its starting point (or, if that is not allowed, another
/// point of its route) with the composition it began with, full hit points, a new loot (see BandLootSetup) and its patrol
/// started over. It never appears during a combat, closer to the caravan than the settings allow, nor, as a rule, in the
/// player's view; a band held back for a few days ignores the view, so a camera zoomed right out cannot keep the map
/// empty. Bands are found at the start of the scene, as BandLootSetup does.
/// </summary>
public class BandRespawner : MonoBehaviour
{
    private const float NavMeshSnapRadius = 3f;

    [SerializeField] private BandRespawnSettings settings;
    [SerializeField] private CampaignClock clock;
    [SerializeField] private CampaignEncounterFlow flow;
    [SerializeField] private Transform caravan;
    [SerializeField] private BandLootSetup lootSetup;
    [Tooltip("Optional: falls back to the main camera.")]
    [SerializeField] private Camera viewCamera;

    private readonly List<Band> bands = new List<Band>();
    private System.Random dice;

    /// <summary>Raised when a band is back on the map.</summary>
    public event Action<UnitTeam> Respawned = delegate { };

    /// <summary>Bands beaten since the start of the scene (a band beaten twice counts twice).</summary>
    public int TotalDefeated { get; private set; }

    /// <summary>Bands waiting to come back.</summary>
    public int Waiting
    {
        get
        {
            int count = 0;
            for (int i = 0; i < bands.Count; i++)
            {
                count += bands[i].ReturnDay > 0 ? 1 : 0;
            }

            return count;
        }
    }

    private sealed class Band
    {
        public UnitTeam Team;
        public PartyRoster Roster;
        public AIUnitController Ai;
        public UnitMover Mover;
        public Vector3 Home;
        public Quaternion Rotation;
        public readonly List<PartyMember> Members = new List<PartyMember>();
        public int ReturnDay;
        public int BlockedDays;
        public int Returns;
    }

    private void Start()
    {
        if (settings == null || clock == null || flow == null || caravan == null || lootSetup == null)
        {
            Debug.LogError("BandRespawner: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        if (viewCamera == null)
        {
            viewCamera = Camera.main;
        }

        dice = new System.Random(settings.Seed != 0 ? settings.Seed : Environment.TickCount);
        IReadOnlyList<UnitTeam> units = UnitTeam.All;
        for (int i = 0; i < units.Count; i++)
        {
            UnitTeam unit = units[i];
            if (unit.Team != Team.Enemy || !unit.TryGetComponent(out PartyRoster roster))
            {
                continue;
            }

            if (!unit.TryGetComponent(out AIUnitController ai) || !unit.TryGetComponent(out UnitMover mover))
            {
                Debug.LogError("BandRespawner: the band '" + unit.name + "' has no AIUnitController or UnitMover: it will not come back.", unit);
                continue;
            }

            var band = new Band { Team = unit, Roster = roster, Ai = ai, Mover = mover, Home = unit.transform.position, Rotation = unit.transform.rotation };
            for (int m = 0; m < roster.Members.Count; m++)
            {
                band.Members.Add(roster.Members[m].Copy());
            }

            bands.Add(band);
        }
    }

    private void OnEnable()
    {
        if (flow != null)
        {
            flow.BandDefeated += OnBandDefeated;
        }

        if (clock != null)
        {
            clock.DayStarted += OnDayStarted;
        }
    }

    private void OnDisable()
    {
        if (flow != null)
        {
            flow.BandDefeated -= OnBandDefeated;
        }

        if (clock != null)
        {
            clock.DayStarted -= OnDayStarted;
        }
    }

    // The flow says a band was beaten: its return day is drawn now, counted from today.
    private void OnBandDefeated(UnitTeam team)
    {
        Band band = Find(team);
        if (band == null)
        {
            Debug.LogError("BandRespawner: the beaten band '" + team.name + "' is not one this component knows: it will not come back.", team);
            return;
        }

        TotalDefeated++;
        band.ReturnDay = clock.Day + dice.Next(settings.MinDays, settings.MaxDays + 1);
        band.BlockedDays = 0;
    }

    private void OnDayStarted(int day)
    {
        for (int i = 0; i < bands.Count; i++)
        {
            Band band = bands[i];
            if (band.ReturnDay > 0 && day >= band.ReturnDay)
            {
                TryRespawn(band);
            }
        }
    }

    private void TryRespawn(Band band)
    {
        // The calendar does not run during a combat (the map is asleep), but the rule is written down all the same.
        if (flow.InCombat)
        {
            return;
        }

        bool ignoreView = band.BlockedDays >= settings.DaysBeforeIgnoringView;
        if (!TryFindPlace(band, ignoreView, out Vector3 place))
        {
            band.BlockedDays++;
            return;
        }

        band.Returns++;
        band.ReturnDay = 0;
        band.BlockedDays = 0;
        band.Roster.ReplaceMembers(band.Members);
        lootSetup.RollFor(band.Roster, band.Returns);
        band.Team.gameObject.SetActive(true);
        band.Mover.PlaceAt(place);
        band.Team.transform.rotation = band.Rotation;
        band.Ai.ResetToPatrol();
        Respawned(band.Team);
    }

    // The first place that is on the NavMesh, far enough from the caravan and (unless the view is ignored) out of the
    // player's view: the starting point first, then the points of the route.
    private bool TryFindPlace(Band band, bool ignoreView, out Vector3 place)
    {
        if (IsAllowed(band.Home, ignoreView, out place))
        {
            return true;
        }

        IReadOnlyList<Transform> route = band.Ai.PatrolPoints;
        for (int i = 0; i < route.Count; i++)
        {
            if (route[i] != null && IsAllowed(route[i].position, ignoreView, out place))
            {
                return true;
            }
        }

        place = default;
        return false;
    }

    private bool IsAllowed(Vector3 point, bool ignoreView, out Vector3 snapped)
    {
        snapped = point;
        if (!NavMesh.SamplePosition(point, out NavMeshHit hit, NavMeshSnapRadius, NavMesh.AllAreas))
        {
            return false;
        }

        snapped = hit.position;
        Vector3 offset = hit.position - caravan.position;
        offset.y = 0f;
        if (offset.magnitude < settings.MinDistanceFromCaravan)
        {
            return false;
        }

        return ignoreView || !IsInView(hit.position);
    }

    // True when the point is on the player's screen, with a margin around it.
    private bool IsInView(Vector3 point)
    {
        if (viewCamera == null)
        {
            Debug.LogError("BandRespawner: there is no camera to tell what the player sees.", this);
            return false;
        }

        Vector3 viewport = viewCamera.WorldToViewportPoint(point);
        float margin = settings.ViewMargin;
        return viewport.z > 0f && viewport.x > -margin && viewport.x < 1f + margin && viewport.y > -margin && viewport.y < 1f + margin;
    }

    private Band Find(UnitTeam team)
    {
        for (int i = 0; i < bands.Count; i++)
        {
            if (bands[i].Team == team)
            {
                return bands[i];
            }
        }

        return null;
    }
}

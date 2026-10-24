using System;
using UnityEngine;

/// <summary>
/// Detects when the caravan and an enemy unit touch. Both stop, the encounter event is raised, and after a short
/// hold the enemy resumes its behavior (later this is where the pre-combat dialog will open).
/// An enemy that just released the caravan is ignored until the two have moved apart, so it cannot re-trigger.
/// Distance test only, no physics.
/// </summary>
public class CampaignEncounters : MonoBehaviour
{
    [SerializeField] private UnitTeam caravanTeam;
    [SerializeField] private UnitMover caravan;
    [SerializeField] private ActionController caravanActions;
    [Tooltip("The two units touch when they are this close, in meters.")]
    [SerializeField] private float encounterDistance = 2.5f;
    [Tooltip("Seconds the enemy stays stopped after an encounter.")]
    [SerializeField] private float holdSeconds = 3f;
    [Tooltip("Seconds between two distance checks.")]
    [SerializeField] private float checkInterval = 0.1f;

    private UnitTeam heldEnemy;
    private AIUnitController heldAi;
    private float releaseTime;
    private UnitTeam ignoredEnemy;
    private float nextCheckTime;

    /// <summary>Raised when the caravan meets an enemy.</summary>
    public event Action<UnitTeam> EncounterStarted = delegate { };
    /// <summary>Raised when the enemy is released after an encounter.</summary>
    public event Action<UnitTeam> EncounterEnded = delegate { };

    private void Start()
    {
        if (caravanTeam == null || caravan == null || caravanActions == null)
        {
            Debug.LogError("CampaignEncounters: caravanTeam, caravan or caravanActions is not assigned.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (Time.time < nextCheckTime)
        {
            return;
        }

        nextCheckTime = Time.time + checkInterval;

        // A band beaten in the combat that just ended is switched off: there is nobody left to release.
        if (heldEnemy != null && !heldEnemy.gameObject.activeInHierarchy)
        {
            heldEnemy = null;
            heldAi = null;
        }

        if (heldEnemy != null)
        {
            if (Time.time >= releaseTime)
            {
                Release();
            }

            return;
        }

        System.Collections.Generic.IReadOnlyList<UnitTeam> units = UnitTeam.All;
        for (int i = 0; i < units.Count; i++)
        {
            UnitTeam other = units[i];
            if (other.Team == caravanTeam.Team)
            {
                continue;
            }

            Vector3 offset = other.transform.position - caravan.transform.position;
            offset.y = 0f;
            float distance = offset.magnitude;

            if (other == ignoredEnemy)
            {
                if (distance > encounterDistance * 2f)
                {
                    ignoredEnemy = null;
                }

                continue;
            }

            if (distance <= encounterDistance)
            {
                Begin(other);
                return;
            }
        }
    }

    private void Begin(UnitTeam enemy)
    {
        heldEnemy = enemy;
        releaseTime = Time.time + holdSeconds;

        // Both stop by cancelling their action, so no action is left running for a unit that stands still.
        caravanActions.Cancel();
        enemy.GetComponent<ActionController>().Cancel();
        heldAi = enemy.GetComponent<AIUnitController>();
        if (heldAi != null)
        {
            heldAi.Interrupt();
        }

        EncounterStarted(enemy);
    }

    private void Release()
    {
        UnitTeam enemy = heldEnemy;
        heldEnemy = null;
        ignoredEnemy = enemy;

        if (heldAi != null)
        {
            heldAi.Resume();
            heldAi = null;
        }

        EncounterEnded(enemy);
    }
}

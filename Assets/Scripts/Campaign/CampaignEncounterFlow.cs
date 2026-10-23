using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Turns an encounter on the campaign map into a combat and brings the result back. When the caravan meets an enemy
/// band, it builds the EncounterData from both rosters, puts the map to sleep (every other root object of this
/// scene is switched off, so its camera, light, NavMesh and units stay exactly as they are) and loads the arena on
/// top. When the arena asks to go back, it unloads it, wakes the map up and applies the result: the survivors keep
/// their hit points, the dead leave the caravan, the beaten band disappears and its loot is gathered. A defeat
/// raises GameOver. This object must stay a root object of its own, outside what it puts to sleep.
/// </summary>
public class CampaignEncounterFlow : MonoBehaviour
{
    [SerializeField] private CampaignEncounters encounters;
    [SerializeField] private PartyRoster caravanParty;
    [SerializeField] private EncounterChannel channel;
    [Tooltip("The combat scene to load (it must be in the build settings).")]
    [SerializeField] private string arenaScene = "Combat_Arena";
    [Tooltip("Seconds of real time between the encounter and the combat, so the player sees what happens.")]
    [Min(0f)] [SerializeField] private float delayBeforeCombat = 1f;

    private readonly List<GameObject> asleep = new List<GameObject>();
    private UnitTeam enemy;
    private bool inCombat;

    /// <summary>Raised when the caravan lost a fight: the campaign is over.</summary>
    public event Action GameOver = delegate { };
    /// <summary>Raised once the map is back, with the result of the fight.</summary>
    public event Action<EncounterData> EncounterResolved = delegate { };

    private void Awake()
    {
        if (encounters == null || caravanParty == null || channel == null)
        {
            Debug.LogError("CampaignEncounterFlow: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        if (transform.parent != null)
        {
            Debug.LogError("CampaignEncounterFlow must be on a root object: it puts the other roots to sleep.", this);
        }

        channel.Clear();
    }

    private void OnEnable()
    {
        if (encounters != null && channel != null)
        {
            encounters.EncounterStarted += OnEncounterStarted;
            channel.ReturnRequested += OnReturnRequested;
        }
    }

    private void OnDisable()
    {
        if (encounters != null && channel != null)
        {
            encounters.EncounterStarted -= OnEncounterStarted;
            channel.ReturnRequested -= OnReturnRequested;
        }
    }

    private void OnEncounterStarted(UnitTeam band)
    {
        if (inCombat)
        {
            return;
        }

        PartyRoster enemyParty = band.GetComponent<PartyRoster>();
        if (enemyParty == null || enemyParty.Members.Count == 0)
        {
            Debug.LogError("CampaignEncounterFlow: " + band.name + " has no party roster, no combat can start.", band);
            return;
        }

        if (caravanParty.Members.Count == 0)
        {
            Debug.LogError("CampaignEncounterFlow: the caravan has nobody left to fight.", this);
            return;
        }

        var encounter = new EncounterData(arenaScene, enemyParty.Loot, true);
        for (int i = 0; i < caravanParty.Members.Count; i++)
        {
            encounter.PlayerSide.Add(caravanParty.Members[i].Copy());
        }

        for (int i = 0; i < enemyParty.Members.Count; i++)
        {
            encounter.EnemySide.Add(enemyParty.Members[i].Copy());
        }

        channel.Begin(encounter);
        enemy = band;
        inCombat = true;
        StartCoroutine(EnterCombat());
    }

    private IEnumerator EnterCombat()
    {
        yield return new WaitForSecondsRealtime(delayBeforeCombat);

        Scene map = gameObject.scene;
        GameObject[] roots = map.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != gameObject && roots[i].activeSelf)
            {
                asleep.Add(roots[i]);
                roots[i].SetActive(false);
            }
        }

        AsyncOperation loading = SceneManager.LoadSceneAsync(arenaScene, LoadSceneMode.Additive);
        if (loading == null)
        {
            Debug.LogError("CampaignEncounterFlow: the arena scene '" + arenaScene + "' could not be loaded (is it in the build settings?).", this);
            WakeMapUp();
            channel.Clear();
            inCombat = false;
            yield break;
        }

        yield return loading;
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(arenaScene));
    }

    private void OnReturnRequested()
    {
        if (inCombat)
        {
            StartCoroutine(LeaveCombat());
        }
    }

    private IEnumerator LeaveCombat()
    {
        EncounterData encounter = channel.Current;
        yield return SceneManager.UnloadSceneAsync(arenaScene);
        WakeMapUp();
        Time.timeScale = 1f;

        if (encounter != null && encounter.HasResult && encounter.Winner == Team.Player)
        {
            caravanParty.ApplyHealth(encounter.PlayerHealthAfter);
            caravanParty.AddLoot(encounter.Loot);
            if (enemy != null)
            {
                Destroy(enemy.gameObject);
            }
        }
        else
        {
            GameOver();
        }

        enemy = null;
        inCombat = false;
        channel.Clear();
        EncounterResolved(encounter);
    }

    private void WakeMapUp()
    {
        for (int i = 0; i < asleep.Count; i++)
        {
            if (asleep[i] != null)
            {
                asleep[i].SetActive(true);
            }
        }

        asleep.Clear();
        SceneManager.SetActiveScene(gameObject.scene);
    }
}

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
/// their hit points, the dead leave the caravan, the beaten band disappears and its loot goes to the wallet. A defeat
/// raises GameOver, and beating the last band raises AllBandsDefeated. The arena is drawn at random from a list
/// for each encounter. This object must stay a root object of its own, outside what it puts to sleep.
/// </summary>
public class CampaignEncounterFlow : MonoBehaviour
{
    [SerializeField] private CampaignEncounters encounters;
    [SerializeField] private PartyRoster caravanParty;
    [Tooltip("The caravan's money: the loot of a beaten band is deposited in it.")]
    [SerializeField] private Wallet wallet;
    [Tooltip("The caravan's cargo: the goods of a beaten band's loot are added to it, as far as they fit.")]
    [SerializeField] private Inventory inventory;
    [SerializeField] private EncounterChannel channel;
    [Tooltip("The combat scenes, one drawn at random for each encounter (they must be in the build settings).")]
    [SerializeField] private string[] arenaScenes = { "Combat_Arena" };
    [Tooltip("Seconds of real time between the encounter and the combat, so the player sees what happens.")]
    [Min(0f)] [SerializeField] private float delayBeforeCombat = 1f;

    private readonly List<GameObject> asleep = new List<GameObject>();
    private UnitTeam enemy;
    private bool inCombat;
    private string arenaScene;

    /// <summary>Raised when the caravan lost a fight: the campaign is over.</summary>
    public event Action GameOver = delegate { };
    /// <summary>Raised when the last enemy band of the map has been beaten.</summary>
    public event Action AllBandsDefeated = delegate { };
    /// <summary>Raised once the map is back, with the result of the fight.</summary>
    public event Action<EncounterData> EncounterResolved = delegate { };

    private void Awake()
    {
        if (encounters == null || caravanParty == null || wallet == null || inventory == null || channel == null || arenaScenes == null || arenaScenes.Length == 0)
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

        arenaScene = arenaScenes[UnityEngine.Random.Range(0, arenaScenes.Length)];
        var encounter = new EncounterData(arenaScene, enemyParty.Loot, true);
        // What of the goods the cargo can take is settled now (the cargo does not change during the fight), so the end
        // screen can say it.
        inventory.PlanAdd(enemyParty.Loot.Goods, encounter.LootTaken, encounter.LootLost);
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

        bool lastBand = false;
        if (encounter != null && encounter.HasResult && encounter.Winner == Team.Player)
        {
            caravanParty.ApplyHealth(encounter.PlayerHealthAfter);
            CollectLoot(encounter);

            lastBand = CountOtherBands(enemy) == 0;
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
        if (lastBand)
        {
            AllBandsDefeated();
        }
    }

    // The money goes to the wallet and the goods that fit (settled when the encounter began) to the cargo. The statistic
    // counts the value really gathered. A refusal here would be a bug, since everything was checked beforehand.
    private void CollectLoot(EncounterData encounter)
    {
        long gathered = 0;
        int uv = encounter.Loot.Uv;
        if (uv > 0)
        {
            WalletResult deposit = wallet.TryDeposit(uv);
            if (deposit == WalletResult.Accepted)
            {
                gathered += uv;
            }
            else
            {
                Debug.LogError("CampaignEncounterFlow: the loot of " + uv + " U.V. was refused by the wallet (" + deposit + ").", this);
            }
        }

        for (int i = 0; i < encounter.LootTaken.Count; i++)
        {
            GoodStack stack = encounter.LootTaken[i];
            InventoryResult added = inventory.TryAdd(stack.Good, stack.Quantity);
            if (added == InventoryResult.Accepted)
            {
                gathered += stack.BaseValue;
            }
            else
            {
                Debug.LogError("CampaignEncounterFlow: " + stack.Quantity + " x " + stack.Good.Id + " of the loot was refused by the cargo (" + added + ").", this);
            }
        }

        caravanParty.RecordLoot((int)System.Math.Min(gathered, int.MaxValue));
    }

    // The enemy bands still on the map, apart from the one just beaten.
    private static int CountOtherBands(UnitTeam beaten)
    {
        int count = 0;
        IReadOnlyList<UnitTeam> units = UnitTeam.All;
        for (int i = 0; i < units.Count; i++)
        {
            if (units[i] != beaten && units[i].Team == Team.Enemy && units[i].GetComponent<PartyRoster>() != null)
            {
                count++;
            }
        }

        return count;
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

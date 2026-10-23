using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Fills the arena from the encounter: one combat unit per member of each side, made from the same unit prefabs as
/// everywhere else, set to the member's type and hit points, lined up at its side's starting point. Enemies get the
/// combat brain with their profile. Then it starts the combat, and when it ends it writes the result into the
/// encounter (hit points of the player's units, losses of both sides).
/// Without an encounter (the arena opened on its own, for testing) it uses its test sides and stays in the arena.
/// </summary>
public class CombatSpawner : MonoBehaviour
{
    [SerializeField] private EncounterChannel channel;
    [SerializeField] private CombatManager combat;
    [SerializeField] private CombatRulesProvider rulesProvider;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject enemyPrefab;
    [Tooltip("Where the player's units start: they line up along its right axis and look along its forward axis.")]
    [SerializeField] private Transform playerStart;
    [SerializeField] private Transform enemyStart;
    [Tooltip("Distance between two units of a line, in meters.")]
    [Min(0.5f)] [SerializeField] private float spacing = 2.5f;
    [Tooltip("Profile of the enemies whose member has none.")]
    [SerializeField] private AIProfile defaultEnemyProfile;

    [Header("Test sides (arena opened on its own)")]
    [SerializeField] private List<PartyMember> testPlayerSide = new List<PartyMember>();
    [SerializeField] private List<PartyMember> testEnemySide = new List<PartyMember>();

    private readonly List<Health> playerUnits = new List<Health>();
    private readonly List<Health> enemyUnits = new List<Health>();
    private EncounterData encounter;

    private void Start()
    {
        if (channel == null || combat == null || rulesProvider == null || playerPrefab == null || enemyPrefab == null
            || playerStart == null || enemyStart == null || defaultEnemyProfile == null)
        {
            Debug.LogError("CombatSpawner: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        // A test encounter left by a previous run of this arena is not reused: it already has its result.
        encounter = channel.Current;
        if (encounter == null || !encounter.ReturnToMap)
        {
            encounter = new EncounterData(gameObject.scene.name, 0, false);
            for (int i = 0; i < testPlayerSide.Count; i++)
            {
                encounter.PlayerSide.Add(testPlayerSide[i].Copy());
            }

            for (int i = 0; i < testEnemySide.Count; i++)
            {
                encounter.EnemySide.Add(testEnemySide[i].Copy());
            }

            channel.Begin(encounter);
        }

        // The units are made inside a switched-off holder, so they can be set up before any of their scripts wakes up.
        var holder = new GameObject("Units");
        holder.SetActive(false);
        SpawnSide(encounter.PlayerSide, playerPrefab, playerStart, holder.transform, false, playerUnits);
        SpawnSide(encounter.EnemySide, enemyPrefab, enemyStart, holder.transform, true, enemyUnits);
        holder.SetActive(true);

        combat.CombatEnded += OnCombatEnded;
        combat.StartCombat();
    }

    private void OnDestroy()
    {
        if (combat != null)
        {
            combat.CombatEnded -= OnCombatEnded;
        }
    }

    private void SpawnSide(List<PartyMember> members, GameObject prefab, Transform start, Transform holder, bool isEnemy,
        List<Health> spawned)
    {
        for (int i = 0; i < members.Count; i++)
        {
            PartyMember member = members[i];
            if (member.definition == null)
            {
                Debug.LogError("CombatSpawner: member '" + member.name + "' has no unit definition.", this);
                spawned.Add(null);
                continue;
            }

            Vector3 position = start.position + start.right * ((i - (members.Count - 1) * 0.5f) * spacing);
            GameObject unit = Instantiate(prefab, position, start.rotation, holder);
            unit.name = member.name;
            unit.GetComponent<UnitStats>().SetDefinition(member.definition);
            Health health = unit.GetComponent<Health>();
            health.SetStartingHealth(member.StartingHealth);
            ActionController actions = unit.GetComponent<ActionController>();
            actions.BindScene(rulesProvider, combat);

            if (isEnemy)
            {
                AICombatBrain brain = unit.AddComponent<AICombatBrain>();
                brain.Configure(unit.GetComponent<AIUnitController>(), actions, unit.GetComponent<UnitStats>(),
                    unit.GetComponent<UnitTeam>(), health, unit.GetComponent<UnitMover>(),
                    member.aiProfile != null ? member.aiProfile : defaultEnemyProfile, combat, rulesProvider);
            }

            spawned.Add(health);
        }
    }

    private void OnCombatEnded(Team? winner)
    {
        var healthAfter = new int[encounter.PlayerSide.Count];
        for (int i = 0; i < healthAfter.Length; i++)
        {
            Health unit = playerUnits[i];
            bool alive = unit != null && !unit.IsDead;
            healthAfter[i] = alive ? unit.Current : 0;
            if (!alive)
            {
                encounter.PlayerLosses.Add(encounter.PlayerSide[i].name);
            }
        }

        for (int i = 0; i < enemyUnits.Count; i++)
        {
            if (enemyUnits[i] == null || enemyUnits[i].IsDead)
            {
                encounter.EnemyLosses.Add(encounter.EnemySide[i].name);
            }
        }

        encounter.SetResult(winner, healthAfter);
    }
}

using System.Collections.Generic;

/// <summary>
/// Everything a combat needs from the campaign map, and everything the map needs back: the two sides (unit types,
/// hit points), the arena to load and the loot (what the band carries, and how much of its goods the caravan's cargo
/// can take); then, once the fight is over, who won, the hit points of the player's units after the fight and the
/// losses of both sides. Plain data, created for one encounter.
/// </summary>
public class EncounterData
{
    public EncounterData(string arenaScene, LootResult loot, bool returnToMap)
    {
        ArenaScene = arenaScene;
        Loot = loot ?? LootResult.Empty;
        ReturnToMap = returnToMap;
    }

    /// <summary>The player's units, in the order of the caravan's roster.</summary>
    public List<PartyMember> PlayerSide { get; } = new List<PartyMember>();
    public List<PartyMember> EnemySide { get; } = new List<PartyMember>();
    public string ArenaScene { get; }
    /// <summary>What the player gets for a victory: U.V. and goods.</summary>
    public LootResult Loot { get; }
    /// <summary>The goods of the loot that fit in the cargo (worked out when the encounter began).</summary>
    public List<GoodStack> LootTaken { get; } = new List<GoodStack>();
    /// <summary>The goods of the loot that the cargo cannot take: they are lost.</summary>
    public List<GoodStack> LootLost { get; } = new List<GoodStack>();
    /// <summary>True when the fight was started from the campaign map and must return there; false in a test arena.</summary>
    public bool ReturnToMap { get; }
    /// <summary>
    /// For the combat test bench only: the player's units are played by the combat AI too, so two AI sides fight and
    /// the result measures the matchup. Off in the game.
    /// </summary>
    public bool PlayerSideByAi { get; set; }

    public bool HasResult { get; private set; }
    public Team? Winner { get; private set; }
    /// <summary>Hit points of each player unit after the fight, in the order of PlayerSide; 0 = dead.</summary>
    public int[] PlayerHealthAfter { get; private set; }
    public List<string> PlayerLosses { get; } = new List<string>();
    public List<string> EnemyLosses { get; } = new List<string>();

    public void SetResult(Team? winner, int[] playerHealthAfter)
    {
        Winner = winner;
        PlayerHealthAfter = playerHealthAfter;
        HasResult = true;
    }
}

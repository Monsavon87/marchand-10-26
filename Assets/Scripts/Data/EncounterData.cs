using System.Collections.Generic;

/// <summary>
/// Everything a combat needs from the campaign map, and everything the map needs back: the two sides (unit types,
/// hit points), the arena to load and the loot; then, once the fight is over, who won, the hit points of the
/// player's units after the fight and the losses of both sides. Plain data, created for one encounter.
/// </summary>
public class EncounterData
{
    public EncounterData(string arenaScene, int loot, bool returnToMap)
    {
        ArenaScene = arenaScene;
        Loot = loot;
        ReturnToMap = returnToMap;
    }

    /// <summary>The player's units, in the order of the caravan's roster.</summary>
    public List<PartyMember> PlayerSide { get; } = new List<PartyMember>();
    public List<PartyMember> EnemySide { get; } = new List<PartyMember>();
    public string ArenaScene { get; }
    /// <summary>What the player gets for a victory (a plain amount for now, not linked to the economy).</summary>
    public int Loot { get; }
    /// <summary>True when the fight was started from the campaign map and must return there; false in a test arena.</summary>
    public bool ReturnToMap { get; }

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

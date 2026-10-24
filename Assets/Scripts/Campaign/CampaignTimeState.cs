/// <summary>What the campaign map's time is doing now (see CampaignPause).</summary>
public enum CampaignTimeState
{
    /// <summary>Nothing is going on: the caravan has no action and the player is not letting time pass. The whole world is still.</summary>
    Frozen,
    /// <summary>The caravan has an action in progress: the world's time runs.</summary>
    Moving,
    /// <summary>The player lets time pass with the caravan standing still: the world's time runs.</summary>
    Waiting,
    /// <summary>The player paused the map.</summary>
    Paused,
    /// <summary>A window (a town, the help page) holds the map still while it is open.</summary>
    Held
}

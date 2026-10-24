using System;
using System.Collections.Generic;

/// <summary>What a simulated fight on the road came to.</summary>
public readonly struct FightOutcome
{
    public FightOutcome(bool happened, bool won, int loot, int unitsLost)
    {
        Happened = happened;
        Won = won;
        Loot = loot;
        UnitsLost = unitsLost;
    }

    public bool Happened { get; }
    public bool Won { get; }
    public int Loot { get; }
    public int UnitsLost { get; }
}

/// <summary>
/// A model of the fights on the road, rolled with the seeded dice (no arena is loaded): each trip has a chance of
/// meeting one of the bands that are left on the map, the chance of winning shrinks with the share of the party that
/// is still standing, a victory pays the band's loot (each band can be beaten once, as in the game) and may cost some
/// units, a defeat ends the campaign (the game over of the map). The party's size is a count here: the real roster is
/// not touched.
/// </summary>
public class SimulationCombat
{
    private readonly SimulationSettings settings;
    private readonly Random dice;
    private readonly List<int> bandLoots;
    private readonly int fullSize;

    public SimulationCombat(SimulationSettings settings, Random dice, IEnumerable<int> bandLoots, int fullSize)
    {
        this.settings = settings;
        this.dice = dice;
        this.bandLoots = new List<int>(bandLoots);
        this.fullSize = Math.Max(1, fullSize);
        Alive = this.fullSize;
    }

    /// <summary>Units of the party still standing.</summary>
    public int Alive { get; private set; }

    /// <summary>Units the party is missing compared to its full size.</summary>
    public int Missing => fullSize - Alive;

    public int BandsLeft => bandLoots.Count;

    /// <summary>One more unit joins the party (recruited in town).</summary>
    public void Replace()
    {
        Alive = Math.Min(fullSize, Alive + 1);
    }

    /// <summary>Rolls the dice for a trip of this many days of travel.</summary>
    public FightOutcome Roll(float travelDays)
    {
        if (bandLoots.Count == 0)
        {
            return default;
        }

        double meet = 1.0 - Math.Pow(1.0 - settings.EncounterChancePerDay, Math.Max(0.0, travelDays));
        if (dice.NextDouble() >= meet)
        {
            return default;
        }

        int band = dice.Next(bandLoots.Count);
        double winChance = settings.BaseWinChance * Alive / fullSize;
        if (dice.NextDouble() >= winChance)
        {
            return new FightOutcome(true, false, 0, 0);
        }

        int loot = bandLoots[band];
        bandLoots.RemoveAt(band);
        int lost = Math.Min(Alive - 1, dice.Next(settings.MaxLossesOnWin + 1));
        Alive -= lost;
        return new FightOutcome(true, true, loot, lost);
    }
}

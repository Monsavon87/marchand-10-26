using System;
using System.Collections.Generic;

/// <summary>What a simulated fight on the road came to.</summary>
public readonly struct FightOutcome
{
    public FightOutcome(bool happened, bool won, LootResult loot, int unitsLost)
    {
        Happened = happened;
        Won = won;
        Loot = loot;
        UnitsLost = unitsLost;
    }

    public bool Happened { get; }
    public bool Won { get; }
    public LootResult Loot { get; }
    public int UnitsLost { get; }
}

/// <summary>
/// A model of the fights on the road, rolled with the seeded dice (no arena is loaded): each trip has a chance of
/// meeting one of the bands that are left on the map, the chance of winning shrinks with the share of the party that
/// is still standing, a victory pays the band's loot, rolled from its table when the campaign starts (each band can be beaten once, as in the game) and may cost some
/// units, a defeat ends the campaign (the game over of the map). The party's size is a count here: the real roster is
/// not touched. A beaten band comes back after a number of days drawn between the respawn settings' minimum and maximum, with
/// a new loot from its table, as on the map (see BandRespawner); without respawn settings it never does.
/// </summary>
public class SimulationCombat
{
    private readonly SimulationSettings settings;
    private readonly Random dice;
    private readonly BandRespawnSettings respawn;
    private readonly List<Band> bands = new List<Band>();
    private readonly int fullSize;

    private sealed class Band
    {
        public LootTable Table;
        public LootResult Loot;
        // The day from which the band is on the map again (0 = it is there).
        public int ReturnDay;
    }

    public SimulationCombat(SimulationSettings settings, BandRespawnSettings respawn, Random dice, IEnumerable<LootTable> bandTables, int fullSize)
    {
        this.settings = settings;
        this.respawn = respawn;
        this.dice = dice;
        // Each band gets its loot when the campaign starts, rolled from its table with the same dice.
        foreach (LootTable table in bandTables)
        {
            bands.Add(new Band { Table = table, Loot = LootRoller.Roll(table, dice) });
        }

        this.fullSize = Math.Max(1, fullSize);
        Alive = this.fullSize;
    }

    /// <summary>Units of the party still standing.</summary>
    public int Alive { get; private set; }

    /// <summary>Units the party is missing compared to its full size.</summary>
    public int Missing => fullSize - Alive;

    /// <summary>Bands on the map on this day.</summary>
    public int BandsOnMap(int day)
    {
        int count = 0;
        for (int i = 0; i < bands.Count; i++)
        {
            count += bands[i].ReturnDay <= day ? 1 : 0;
        }

        return count;
    }

    /// <summary>One more unit joins the party (recruited in town).</summary>
    public void Replace()
    {
        Alive = Math.Min(fullSize, Alive + 1);
    }

    /// <summary>Rolls the dice for a trip of this many days of travel, beginning on this day of the run.</summary>
    public FightOutcome Roll(float travelDays, int day)
    {
        var present = new List<Band>();
        for (int i = 0; i < bands.Count; i++)
        {
            if (bands[i].ReturnDay <= day)
            {
                present.Add(bands[i]);
            }
        }

        if (present.Count == 0)
        {
            return default;
        }

        double meet = 1.0 - Math.Pow(1.0 - settings.EncounterChancePerDay, Math.Max(0.0, travelDays));
        if (dice.NextDouble() >= meet)
        {
            return default;
        }

        Band band = present[dice.Next(present.Count)];
        double winChance = settings.BaseWinChance * Alive / fullSize;
        if (dice.NextDouble() >= winChance)
        {
            return new FightOutcome(true, false, LootResult.Empty, 0);
        }

        LootResult loot = band.Loot;
        if (respawn != null)
        {
            // It comes back after its delay (counted from the end of the trip), with another loot.
            band.ReturnDay = day + (int)Math.Ceiling(travelDays) + dice.Next(respawn.MinDays, respawn.MaxDays + 1);
            band.Loot = LootRoller.Roll(band.Table, dice);
        }
        else
        {
            band.ReturnDay = int.MaxValue;
        }
        int lost = Math.Min(Alive - 1, dice.Next(settings.MaxLossesOnWin + 1));
        Alive -= lost;
        return new FightOutcome(true, true, loot, lost);
    }
}

using System;
using System.Collections.Generic;

/// <summary>What a simulated fight on the road came to.</summary>
public readonly struct FightOutcome
{
    public FightOutcome(bool happened, bool won, LootResult loot)
    {
        Happened = happened;
        Won = won;
        Loot = loot;
    }

    public bool Happened { get; }
    public bool Won { get; }
    public LootResult Loot { get; }
}

/// <summary>One enemy band of the map, as the simulator sees it: its name, its composition and its loot table.</summary>
public class SimulatedBand
{
    public string Name;
    /// <summary>Its units as a composition key (see CombatOddsTable.KeyOf).</summary>
    public string Key;
    public LootTable Table;
    public LootResult Loot;
    // The day from which the band is on the map again (0 = it is there).
    public int ReturnDay;
}

/// <summary>
/// The bands on the road, rolled with the seeded dice (no arena is loaded): each trip has a chance of meeting one of
/// the bands that are on the map (Meet); whether to fight it is the merchant's choice (the simulator decides); a
/// fight is won with the chance it is given (Fight), and a victory pays the band's loot, rolled from its table. A
/// defeat ends the campaign (the game over of the map). The wounds of a victory are the group's business
/// (SimulatedParty). A beaten band comes back after a number of days drawn between the respawn settings' minimum and
/// maximum, with a new loot from its table, as on the map (see BandRespawner); without respawn settings it never does.
/// </summary>
public class SimulationCombat
{
    private readonly SimulationSettings settings;
    private readonly Random dice;
    private readonly BandRespawnSettings respawn;
    private readonly List<SimulatedBand> bands = new List<SimulatedBand>();
    private readonly List<SimulatedBand> present = new List<SimulatedBand>();

    public SimulationCombat(SimulationSettings settings, BandRespawnSettings respawn, Random dice, IEnumerable<SimulatedBand> mapBands)
    {
        this.settings = settings;
        this.respawn = respawn;
        this.dice = dice;
        // Each band gets its loot when the campaign starts, rolled from its table with the same dice.
        foreach (SimulatedBand band in mapBands)
        {
            bands.Add(new SimulatedBand { Name = band.Name, Key = band.Key, Table = band.Table, Loot = LootRoller.Roll(band.Table, dice) });
        }
    }

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

    /// <summary>
    /// Rolls the dice for a trip of this many days of travel, beginning on this day of the run: the band met on the
    /// way, or null when none is.
    /// </summary>
    public SimulatedBand Meet(float travelDays, int day)
    {
        present.Clear();
        for (int i = 0; i < bands.Count; i++)
        {
            if (bands[i].ReturnDay <= day)
            {
                present.Add(bands[i]);
            }
        }

        if (present.Count == 0)
        {
            return null;
        }

        double meet = 1.0 - Math.Pow(1.0 - settings.EncounterChancePerDay, Math.Max(0.0, travelDays));
        if (dice.NextDouble() >= meet)
        {
            return null;
        }

        return present[dice.Next(present.Count)];
    }

    /// <summary>
    /// The fight against a band met on a trip of this many days beginning on this day, won with this chance. A beaten
    /// band leaves the map and comes back later with another loot.
    /// </summary>
    public FightOutcome Fight(SimulatedBand band, double winChance, float travelDays, int day)
    {
        if (dice.NextDouble() >= Math.Max(0.0, Math.Min(1.0, winChance)))
        {
            return new FightOutcome(true, false, LootResult.Empty);
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

        return new FightOutcome(true, true, loot);
    }
}

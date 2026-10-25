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

/// <summary>
/// A model of the fights on the road, rolled with the seeded dice (no arena is loaded): each trip has a chance of
/// meeting one of the bands that are on the map, the chance of winning is the base chance times the strength of the
/// group (its hit points over those of a full group: see SimulatedParty.Strength), a victory pays the band's loot,
/// rolled from its table, a defeat ends the campaign (the game over of the map). The wounds of a victory are the
/// group's business (SimulatedParty.TakeVictoryDamage). A beaten band comes back after a number of days drawn between
/// the respawn settings' minimum and maximum, with a new loot from its table, as on the map (see BandRespawner);
/// without respawn settings it never does.
/// </summary>
public class SimulationCombat
{
    private readonly SimulationSettings settings;
    private readonly double baseWinChance;
    private readonly Random dice;
    private readonly BandRespawnSettings respawn;
    private readonly List<Band> bands = new List<Band>();
    private readonly List<Band> present = new List<Band>();

    private sealed class Band
    {
        public LootTable Table;
        public LootResult Loot;
        // The day from which the band is on the map again (0 = it is there).
        public int ReturnDay;
    }

    /// <summary>'baseWinChance' is the chance to win with a full group (the settings' one, or another for a sensitivity run).</summary>
    public SimulationCombat(SimulationSettings settings, double baseWinChance, BandRespawnSettings respawn, Random dice, IEnumerable<LootTable> bandTables)
    {
        this.settings = settings;
        this.baseWinChance = baseWinChance;
        this.respawn = respawn;
        this.dice = dice;
        // Each band gets its loot when the campaign starts, rolled from its table with the same dice.
        foreach (LootTable table in bandTables)
        {
            bands.Add(new Band { Table = table, Loot = LootRoller.Roll(table, dice) });
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

    /// <summary>The chance to win a fight for a group of this strength (0 to 1).</summary>
    public double WinChance(double strength)
    {
        return baseWinChance * Math.Max(0.0, Math.Min(1.0, strength));
    }

    /// <summary>
    /// Rolls the dice for a trip of this many days of travel, beginning on this day of the run, for a group of this
    /// strength (0 to 1).
    /// </summary>
    public FightOutcome Roll(float travelDays, int day, double strength)
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
            return default;
        }

        double meet = 1.0 - Math.Pow(1.0 - settings.EncounterChancePerDay, Math.Max(0.0, travelDays));
        if (dice.NextDouble() >= meet)
        {
            return default;
        }

        Band band = present[dice.Next(present.Count)];
        if (dice.NextDouble() >= WinChance(strength))
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

using System;
using System.Collections.Generic;

/// <summary>
/// The merchant's group in the economy simulator: units with their definition (maximum hit points, daily wage) and
/// their hit points, copied from the real roster when a run starts. It follows the game's rules where they exist
/// (natural healing: HealingRules; hospital price: HealingRules.Price) and the simulator's hypotheses elsewhere (the
/// damage of a won fight). Pure data and rules: it touches no scene, no wallet; the simulator pays.
/// </summary>
public class SimulatedParty
{
    private sealed class Unit
    {
        public UnitDefinition Definition;
        public int Health;

        public int Max => Definition != null ? Definition.MaxHealth : 0;
    }

    private readonly List<Unit> units = new List<Unit>();

    /// <summary>A group made of these units, at full health.</summary>
    public SimulatedParty(IEnumerable<UnitDefinition> definitions)
    {
        foreach (UnitDefinition definition in definitions)
        {
            Add(definition);
        }
    }

    public int Count => units.Count;

    /// <summary>What the group costs each day (the sum of the wages).</summary>
    public int DailyWage
    {
        get
        {
            int total = 0;
            for (int i = 0; i < units.Count; i++)
            {
                total += units[i].Definition != null ? units[i].Definition.DailyWage : 0;
            }

            return total;
        }
    }

    /// <summary>Hit points of all the units together.</summary>
    public int TotalHealth
    {
        get
        {
            int total = 0;
            for (int i = 0; i < units.Count; i++)
            {
                total += units[i].Health;
            }

            return total;
        }
    }

    /// <summary>Maximum hit points of all the units together.</summary>
    public int TotalMaxHealth
    {
        get
        {
            int total = 0;
            for (int i = 0; i < units.Count; i++)
            {
                total += units[i].Max;
            }

            return total;
        }
    }

    public int HealthOf(int index) => units[index].Health;
    public int MaxHealthOf(int index) => units[index].Max;

    /// <summary>A new unit joins, at full health (a recruit).</summary>
    public void Add(UnitDefinition definition)
    {
        if (definition == null)
        {
            return;
        }

        units.Add(new Unit { Definition = definition, Health = definition.MaxHealth });
    }

    /// <summary>
    /// The strength of the group against a band, between 0 and 1: its hit points over those of a full group of
    /// 'fullHealth' hit points.
    /// </summary>
    public double Strength(int fullHealth)
    {
        return fullHealth > 0 ? Math.Min(1.0, TotalHealth / (double)fullHealth) : 0.0;
    }

    /// <summary>
    /// The wounds of a won fight: each unit loses a share of its maximum drawn between minPercent and maxPercent (at
    /// least 1 hit point); a unit at 0 dies and leaves. A won fight always leaves one unit standing (with 1 hit point
    /// at least). Returns the number of dead.
    /// </summary>
    public int TakeVictoryDamage(Random dice, int minPercent, int maxPercent)
    {
        int low = Math.Max(0, Math.Min(minPercent, maxPercent));
        int high = Math.Max(low, maxPercent);
        for (int i = 0; i < units.Count; i++)
        {
            int percent = dice.Next(low, high + 1);
            int damage = Math.Max(1, (int)Math.Round(units[i].Max * percent / 100.0));
            units[i].Health = Math.Max(0, units[i].Health - damage);
        }

        // A won fight: somebody is still standing.
        if (units.Count > 0 && TotalHealth == 0)
        {
            units[0].Health = 1;
        }

        int dead = 0;
        for (int i = units.Count - 1; i >= 0; i--)
        {
            if (units[i].Health <= 0)
            {
                units.RemoveAt(i);
                dead++;
            }
        }

        return dead;
    }

    /// <summary>One day of natural healing for every unit (the game's rule). Returns the hit points given back.</summary>
    public int RegenerateDay(HealingSettings healing)
    {
        int given = 0;
        for (int i = 0; i < units.Count; i++)
        {
            int after = HealingRules.NextHealth(units[i].Health, units[i].Max, healing.RegenPercentPerDay, healing.RegenCapPercent);
            given += after - units[i].Health;
            units[i].Health = after;
        }

        return given;
    }

    /// <summary>
    /// The hospital for the units under 'thresholdPercent' of their maximum: the price to bring them all back to full
    /// (the game's price per hit point), without healing anyone. 0 when nobody is under the threshold.
    /// </summary>
    public long HospitalPrice(int thresholdPercent, int pricePerHitPoint)
    {
        long price = 0;
        for (int i = 0; i < units.Count; i++)
        {
            if (NeedsHospital(i, thresholdPercent))
            {
                price += HealingRules.Price(HealingRules.Missing(units[i].Health, units[i].Max), pricePerHitPoint);
            }
        }

        return price;
    }

    /// <summary>Brings the units under the threshold back to full health. Returns the hit points given back.</summary>
    public int Heal(int thresholdPercent)
    {
        int given = 0;
        for (int i = 0; i < units.Count; i++)
        {
            if (NeedsHospital(i, thresholdPercent))
            {
                given += units[i].Max - units[i].Health;
                units[i].Health = units[i].Max;
            }
        }

        return given;
    }

    /// <summary>A short text of the group, to compare two runs ("Fantassin 40/40, Archer 12/25").</summary>
    public string Describe()
    {
        var text = new System.Text.StringBuilder();
        for (int i = 0; i < units.Count; i++)
        {
            if (i > 0)
            {
                text.Append(", ");
            }

            text.Append(units[i].Definition != null ? units[i].Definition.name : "?").Append(' ').Append(units[i].Health).Append('/').Append(units[i].Max);
        }

        return text.ToString();
    }

    private bool NeedsHospital(int index, int thresholdPercent)
    {
        Unit unit = units[index];
        return unit.Health < unit.Max && unit.Health * 100L < (long)unit.Max * thresholdPercent;
    }
}

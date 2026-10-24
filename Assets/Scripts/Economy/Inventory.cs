using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The goods a convoy carries: a whole quantity per good, within the convoy's capacity in mass and in volume.
/// TryAdd and TryRemove are the only ways to change the stock, and each is all-or-nothing: a refused operation
/// changes nothing and says why. It also keeps what each good cost
/// (the total paid for the units carried, at the average of what was paid: 5 units at 200 then 5 at 300 cost 2 500 in all, 250
/// each), so a sale can say what it earned over what it cost. Goods that cost nothing (a band's loot) are added with a cost of
/// 0 and bring the average down. Pure data plus its rules; it knows nothing about prices, towns or the interface.
/// </summary>
public class Inventory : MonoBehaviour
{
    // A float sum can fall a hair under the limit it should reach exactly (3 x 0.1 L): this slack absorbs it.
    private const float Tolerance = 0.0001f;

    [SerializeField] private GoodCatalog catalog;
    [SerializeField] private CargoCapacity capacity;

    private readonly Dictionary<GoodDefinition, int> quantities = new Dictionary<GoodDefinition, int>();
    private readonly Dictionary<GoodDefinition, long> costs = new Dictionary<GoodDefinition, long>();
    private bool configured;

    /// <summary>Raised after the stock changed (an add or a remove that was accepted).</summary>
    public event Action Changed = delegate { };

    public float TotalMass { get; private set; }
    public float TotalVolume { get; private set; }
    public CargoCapacity Capacity => capacity;

    private void Awake()
    {
        if (catalog == null || capacity == null)
        {
            Debug.LogError("Inventory: the catalog or the capacity is not assigned.", this);
            return;
        }

        configured = true;
    }

    /// <summary>How many units of this good are carried (0 when none).</summary>
    public int GetQuantity(GoodDefinition good)
    {
        if (good == null)
        {
            return 0;
        }

        return quantities.TryGetValue(good, out int quantity) ? quantity : 0;
    }

    /// <summary>What the units of this good carried cost in all, in U.V. (0 when none, or when they cost nothing).</summary>
    public long GetTotalCost(GoodDefinition good)
    {
        return good != null && costs.TryGetValue(good, out long cost) ? cost : 0L;
    }

    /// <summary>
    /// What 'quantity' units of this good cost, at the average of what the units carried cost (the whole cost when
    /// 'quantity' is all of them, rounded down otherwise). 0 when none are carried. Changes nothing.
    /// </summary>
    public long CostOf(GoodDefinition good, int quantity)
    {
        int owned = GetQuantity(good);
        if (owned <= 0 || quantity <= 0)
        {
            return 0L;
        }

        return quantity >= owned ? GetTotalCost(good) : GetTotalCost(good) * quantity / owned;
    }

    /// <summary>
    /// The most units of this good that still fit, given the room left in mass AND in volume (0 when none, or
    /// when the good is not valid).
    /// </summary>
    public int MaxAddable(GoodDefinition good)
    {
        return Mathf.Min(MaxAddableByMass(good), MaxAddableByVolume(good));
    }

    /// <summary>
    /// Works out what of a list of goods would fit if all of them were added, without adding anything: the goods are
    /// taken in order of value (the dearest stack first), each as many units as the room left in mass AND in volume
    /// allows, so the cheapest are the ones left out. 'accepted' and 'refused' are filled (cleared first) with the
    /// units that fit and those that do not. The two together always make the list that was asked.
    /// </summary>
    public void PlanAdd(IReadOnlyList<GoodStack> wanted, List<GoodStack> accepted, List<GoodStack> refused)
    {
        accepted.Clear();
        refused.Clear();
        var sorted = new List<GoodStack>(wanted);
        // Insertion sort: stable, so goods of equal value keep their order.
        for (int i = 1; i < sorted.Count; i++)
        {
            GoodStack item = sorted[i];
            int j = i - 1;
            while (j >= 0 && sorted[j].BaseValue < item.BaseValue)
            {
                sorted[j + 1] = sorted[j];
                j--;
            }

            sorted[j + 1] = item;
        }

        float massLeft = configured ? capacity.MaxMass - TotalMass : 0f;
        float volumeLeft = configured ? capacity.MaxVolume - TotalVolume : 0f;
        for (int i = 0; i < sorted.Count; i++)
        {
            GoodStack stack = sorted[i];
            int fits = IsUsable(stack.Good)
                ? Mathf.Min(stack.Quantity, Mathf.Min(Fit(massLeft, stack.Good.MassPerUnit), Fit(volumeLeft, stack.Good.VolumePerUnit)))
                : 0;
            if (fits > 0)
            {
                accepted.Add(new GoodStack(stack.Good, fits));
                massLeft -= fits * stack.Good.MassPerUnit;
                volumeLeft -= fits * stack.Good.VolumePerUnit;
            }

            if (fits < stack.Quantity)
            {
                refused.Add(new GoodStack(stack.Good, stack.Quantity - fits));
            }
        }
    }

    /// <summary>Would adding units of a good be accepted, and if not why? Changes nothing.</summary>
    public InventoryResult CheckAdd(GoodDefinition good, int quantity)
    {
        InventoryResult invalid = CheckRequest(good, quantity);
        if (invalid != InventoryResult.Accepted)
        {
            return invalid;
        }

        if (quantity > MaxAddableByMass(good))
        {
            return InventoryResult.MassExceeded;
        }

        if (quantity > MaxAddableByVolume(good))
        {
            return InventoryResult.VolumeExceeded;
        }

        return InventoryResult.Accepted;
    }

    /// <summary>
    /// Adds units of a good, with what they cost in all ('cost', in U.V.; 0 for goods that cost nothing). Accepted, or
    /// the precise reason why nothing was added.
    /// </summary>
    public InventoryResult TryAdd(GoodDefinition good, int quantity, int cost = 0)
    {
        InventoryResult check = CheckAdd(good, quantity);
        if (check != InventoryResult.Accepted)
        {
            return check;
        }

        if (cost < 0)
        {
            return InventoryResult.InvalidQuantity;
        }

        quantities[good] = GetQuantity(good) + quantity;
        costs[good] = GetTotalCost(good) + cost;
        Recompute();
        return InventoryResult.Accepted;
    }

    /// <summary>Removes units of a good. Accepted, or the reason why nothing was removed.</summary>
    public InventoryResult TryRemove(GoodDefinition good, int quantity)
    {
        return TryRemove(good, quantity, out _);
    }

    /// <summary>
    /// Removes units of a good, and says what they cost (see CostOf): that cost leaves the total, so what stays keeps
    /// the same average. Accepted, or the reason why nothing was removed ('removedCost' is then 0).
    /// </summary>
    public InventoryResult TryRemove(GoodDefinition good, int quantity, out long removedCost)
    {
        removedCost = 0L;
        InventoryResult invalid = CheckRequest(good, quantity);
        if (invalid != InventoryResult.Accepted)
        {
            return invalid;
        }

        int owned = GetQuantity(good);
        if (quantity > owned)
        {
            return InventoryResult.NotEnough;
        }

        removedCost = CostOf(good, quantity);
        if (quantity == owned)
        {
            quantities.Remove(good);
            costs.Remove(good);
        }
        else
        {
            quantities[good] = owned - quantity;
            costs[good] = GetTotalCost(good) - removedCost;
        }

        Recompute();
        return InventoryResult.Accepted;
    }

    private InventoryResult CheckRequest(GoodDefinition good, int quantity)
    {
        if (!configured)
        {
            return InventoryResult.NotConfigured;
        }

        if (good == null || !catalog.TryGetGood(good.Id, out GoodDefinition known) || known != good)
        {
            return InventoryResult.UnknownGood;
        }

        return quantity > 0 ? InventoryResult.Accepted : InventoryResult.InvalidQuantity;
    }

    private int MaxAddableByMass(GoodDefinition good)
    {
        if (!IsUsable(good))
        {
            return 0;
        }

        return Fit(capacity.MaxMass - TotalMass, good.MassPerUnit);
    }

    private int MaxAddableByVolume(GoodDefinition good)
    {
        if (!IsUsable(good))
        {
            return 0;
        }

        return Fit(capacity.MaxVolume - TotalVolume, good.VolumePerUnit);
    }

    private bool IsUsable(GoodDefinition good)
    {
        return configured && good != null && good.MassPerUnit > 0f && good.VolumePerUnit > 0f;
    }

    private static int Fit(float room, float perUnit)
    {
        if (room <= 0f)
        {
            return 0;
        }

        float count = Mathf.Floor((room + Tolerance) / perUnit);
        return (int)Mathf.Min(count, int.MaxValue);
    }

    // Totals are recomputed from the stock at each change (a handful of goods) so no rounding error piles up.
    private void Recompute()
    {
        float mass = 0f;
        float volume = 0f;
        foreach (KeyValuePair<GoodDefinition, int> entry in quantities)
        {
            mass += entry.Key.MassPerUnit * entry.Value;
            volume += entry.Key.VolumePerUnit * entry.Value;
        }

        TotalMass = mass;
        TotalVolume = volume;
        Changed();
    }
}

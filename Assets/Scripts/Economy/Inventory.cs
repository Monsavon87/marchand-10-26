using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The goods a convoy carries: a whole quantity per good, within the convoy's capacity in mass and in volume.
/// TryAdd and TryRemove are the only ways to change the stock, and each is all-or-nothing: a refused operation
/// changes nothing and says why. Pure data plus its rules; it knows nothing about prices, towns or the interface.
/// </summary>
public class Inventory : MonoBehaviour
{
    // A float sum can fall a hair under the limit it should reach exactly (3 x 0.1 L): this slack absorbs it.
    private const float Tolerance = 0.0001f;

    [SerializeField] private GoodCatalog catalog;
    [SerializeField] private CargoCapacity capacity;

    private readonly Dictionary<GoodDefinition, int> quantities = new Dictionary<GoodDefinition, int>();
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

    /// <summary>
    /// The most units of this good that still fit, given the room left in mass AND in volume (0 when none, or
    /// when the good is not valid).
    /// </summary>
    public int MaxAddable(GoodDefinition good)
    {
        return Mathf.Min(MaxAddableByMass(good), MaxAddableByVolume(good));
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

    /// <summary>Adds units of a good. Accepted, or the precise reason why nothing was added.</summary>
    public InventoryResult TryAdd(GoodDefinition good, int quantity)
    {
        InventoryResult check = CheckAdd(good, quantity);
        if (check != InventoryResult.Accepted)
        {
            return check;
        }

        quantities[good] = GetQuantity(good) + quantity;
        Recompute();
        return InventoryResult.Accepted;
    }

    /// <summary>Removes units of a good. Accepted, or the reason why nothing was removed.</summary>
    public InventoryResult TryRemove(GoodDefinition good, int quantity)
    {
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

        if (quantity == owned)
        {
            quantities.Remove(good);
        }
        else
        {
            quantities[good] = owned - quantity;
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

/// <summary>The outcome of an operation on an Inventory. Anything but Accepted means nothing was changed.</summary>
public enum InventoryResult
{
    Accepted,
    /// <summary>The quantity is 0 or negative.</summary>
    InvalidQuantity,
    /// <summary>The good is missing, or is not one of the catalog's.</summary>
    UnknownGood,
    /// <summary>Adding would exceed the mass limit.</summary>
    MassExceeded,
    /// <summary>Adding would exceed the volume limit.</summary>
    VolumeExceeded,
    /// <summary>Removing more than is owned.</summary>
    NotEnough,
    /// <summary>The inventory has no catalog or no capacity assigned.</summary>
    NotConfigured
}

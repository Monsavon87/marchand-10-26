using UnityEngine;

/// <summary>
/// The queue of orders outside combat (the campaign map): how many orders a unit can line up behind the current one
/// with Shift + click. Pure data.
/// </summary>
[CreateAssetMenu(fileName = "OrderQueueSettings", menuName = "Marchand/Order Queue Settings")]
public class OrderQueueSettings : ScriptableObject
{
    [Tooltip("Orders that can wait behind the current one.")]
    [Range(1, 16)] [SerializeField] private int maxOrders = 4;

    public int MaxOrders => maxOrders;
}

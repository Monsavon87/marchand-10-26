using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Debug only: keys to test an inventory. Keys 1 to 0 add the catalog's 1st to 10th good; Shift + the key removes
/// it. Each try is written in the console with its result and the totals. Remove or disable it once the
/// trade interface exists.
/// </summary>
public class InventoryDebugInput : MonoBehaviour
{
    private static readonly Key[] GoodKeys =
    {
        Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
        Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9, Key.Digit0
    };

    [SerializeField] private Inventory inventory;
    [SerializeField] private GoodCatalog catalog;
    [Tooltip("Units added or removed by each key press.")]
    [Min(1)] [SerializeField] private int quantity = 1;
    [Tooltip("The digits do nothing while one of these keys is held, so another debug input can use the same digits.")]
    [SerializeField] private Key[] ignoreWhileHeld = System.Array.Empty<Key>();

    private void Start()
    {
        if (inventory == null || catalog == null)
        {
            Debug.LogError("InventoryDebugInput: inventory or catalog is not assigned.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        for (int i = 0; i < ignoreWhileHeld.Length; i++)
        {
            if (keyboard[ignoreWhileHeld[i]].isPressed)
            {
                return;
            }
        }

        bool shift = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        for (int i = 0; i < GoodKeys.Length && i < catalog.Goods.Count; i++)
        {
            if (keyboard[GoodKeys[i]].wasPressedThisFrame)
            {
                Try(catalog.Goods[i], shift);
            }
        }
    }

    private void Try(GoodDefinition good, bool remove)
    {
        if (good == null)
        {
            return;
        }

        InventoryResult result = remove ? inventory.TryRemove(good, quantity) : inventory.TryAdd(good, quantity);
        Debug.Log("[Debug inventory] " + (remove ? "remove " : "add ") + quantity + " " + good.DisplayName + " -> " + result
            + " | owned " + inventory.GetQuantity(good)
            + " | mass " + inventory.TotalMass.ToString("0.###") + "/" + inventory.Capacity.MaxMass
            + " | volume " + inventory.TotalVolume.ToString("0.###") + "/" + inventory.Capacity.MaxVolume
            + " | max addable " + inventory.MaxAddable(good));
    }
}

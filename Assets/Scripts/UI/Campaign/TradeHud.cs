using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The caravan's money and cargo on the map screen: the U.V. and how full the cargo is, in kilograms and liters, with
/// a bar for each. It follows the wallet and the inventory through their events, so nothing runs each frame.
/// </summary>
public class TradeHud : MonoBehaviour
{
    [SerializeField] private Wallet wallet;
    [SerializeField] private Inventory inventory;
    [SerializeField] private Text label;
    [Tooltip("The bar of the mass: its width follows how full the cargo is (an anchor, 0 to 1).")]
    [SerializeField] private RectTransform massFill;
    [SerializeField] private RectTransform volumeFill;

    private void Start()
    {
        if (wallet == null || inventory == null || label == null || massFill == null || volumeFill == null)
        {
            Debug.LogError("TradeHud: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        Refresh();
    }

    private void OnEnable()
    {
        if (wallet != null && inventory != null)
        {
            wallet.Changed += Refresh;
            inventory.Changed += Refresh;
        }
    }

    private void OnDisable()
    {
        if (wallet != null && inventory != null)
        {
            wallet.Changed -= Refresh;
            inventory.Changed -= Refresh;
        }
    }

    private void Refresh()
    {
        CargoCapacity capacity = inventory.Capacity;
        label.text = MoneyFormat.Unit + " : " + MoneyFormat.Number(wallet.Balance)
            + "     Cargaison : " + inventory.TotalMass.ToString("0.#", CultureInfo.InvariantCulture) + " / " + capacity.MaxMass.ToString("0.#", CultureInfo.InvariantCulture) + " kg"
            + "     " + inventory.TotalVolume.ToString("0.#", CultureInfo.InvariantCulture) + " / " + capacity.MaxVolume.ToString("0.#", CultureInfo.InvariantCulture) + " L";
        SetFill(massFill, inventory.TotalMass / capacity.MaxMass);
        SetFill(volumeFill, inventory.TotalVolume / capacity.MaxVolume);
    }

    private static void SetFill(RectTransform fill, float ratio)
    {
        fill.anchorMax = new Vector2(Mathf.Clamp01(ratio), fill.anchorMax.y);
    }
}

using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The cargo window: what the caravan carries (quantity, mass and volume of each good) and the totals against its
/// capacity. The same key opens and closes it, and the escape key closes it too. It works on the map and over a town
/// window, and holds the map still while it is open, so it can be read without a patrol arriving.
/// </summary>
public class InventoryPanel : MonoBehaviour
{
    [SerializeField] private Inventory inventory;
    [SerializeField] private GoodCatalog catalog;
    [SerializeField] private CampaignPause pause;
    [Tooltip("The whole window, with its dimmed background: hidden until the key is pressed.")]
    [SerializeField] private GameObject root;
    [SerializeField] private Text namesText;
    [SerializeField] private Text quantitiesText;
    [SerializeField] private Text massText;
    [SerializeField] private Text volumeText;
    [SerializeField] private Text totalsText;
    [SerializeField] private Button closeButton;
    [Tooltip("Keys are physical positions (US layout).")]
    [SerializeField] private Key toggleKey = Key.I;
    [SerializeField] private Key closeKey = Key.Escape;

    private readonly StringBuilder names = new StringBuilder(256);
    private readonly StringBuilder quantities = new StringBuilder(128);
    private readonly StringBuilder masses = new StringBuilder(128);
    private readonly StringBuilder volumes = new StringBuilder(128);

    public bool IsOpen => root != null && root.activeSelf;

    private void Start()
    {
        if (inventory == null || catalog == null || pause == null || root == null || namesText == null || quantitiesText == null
            || massText == null || volumeText == null || totalsText == null || closeButton == null)
        {
            Debug.LogError("InventoryPanel: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        root.SetActive(false);
    }

    private void OnEnable()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }
    }

    private void OnDisable()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
        }

        if (IsOpen)
        {
            Close();
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard[toggleKey].wasPressedThisFrame)
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }
        else if (IsOpen && keyboard[closeKey].wasPressedThisFrame)
        {
            Close();
        }
    }

    private void Open()
    {
        root.SetActive(true);
        UiLayering.BringToFront(root.transform);
        pause.SetHeld(this, true);
        inventory.Changed += Refresh;
        Refresh();
    }

    private void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        inventory.Changed -= Refresh;
        root.SetActive(false);
        pause.SetHeld(this, false);
    }

    private void Refresh()
    {
        names.Clear();
        quantities.Clear();
        masses.Clear();
        volumes.Clear();
        int kinds = 0;
        for (int i = 0; i < catalog.Goods.Count; i++)
        {
            GoodDefinition good = catalog.Goods[i];
            int owned = inventory.GetQuantity(good);
            if (owned <= 0)
            {
                continue;
            }

            kinds++;
            names.Append(good.DisplayName).Append('\n');
            quantities.Append(MoneyFormat.Number(owned)).Append(' ').Append(good.Unit).Append('\n');
            masses.Append((owned * good.MassPerUnit).ToString("0.#", CultureInfo.InvariantCulture)).Append(" kg\n");
            volumes.Append((owned * good.VolumePerUnit).ToString("0.#", CultureInfo.InvariantCulture)).Append(" L\n");
        }

        if (kinds == 0)
        {
            names.Append("La cargaison est vide.");
        }

        namesText.text = names.ToString();
        quantitiesText.text = quantities.ToString();
        massText.text = masses.ToString();
        volumeText.text = volumes.ToString();

        CargoCapacity capacity = inventory.Capacity;
        totalsText.text = "Masse : " + inventory.TotalMass.ToString("0.#", CultureInfo.InvariantCulture) + " / " + capacity.MaxMass.ToString("0.#", CultureInfo.InvariantCulture)
            + " kg\nVolume : " + inventory.TotalVolume.ToString("0.#", CultureInfo.InvariantCulture) + " / " + capacity.MaxVolume.ToString("0.#", CultureInfo.InvariantCulture) + " L";
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The town's main window, opened when the caravan reaches a town: the generic information (the
/// town, a note when the group is wounded, the group, the money and the cargo) and one button per town function (market, recruitment...), each
/// opening its own window in place of this one; a window's own button brings the player back here. The map is
/// paused while any of it is open, so no patrol reaches the caravan meanwhile. What each function does is its own
/// window's business: this one only shows the town and goes from window to window.
/// </summary>
public class TownPanel : MonoBehaviour
{
    [SerializeField] private TownRest townRest;
    [Tooltip("The whole main window: hidden until the caravan reaches a town.")]
    [SerializeField] private GameObject root;
    [SerializeField] private Text titleText;
    [SerializeField] private Text subtitleText;
    [SerializeField] private Text groupText;
    [SerializeField] private Text summaryText;
    [SerializeField] private PartyRoster party;
    [SerializeField] private PartyCapacity capacity;
    [SerializeField] private Wallet wallet;
    [SerializeField] private Inventory inventory;
    [Tooltip("The windows of the town functions: the main window shows one button for each, in this order.")]
    [SerializeField] private List<TownFunctionWindow> functions = new List<TownFunctionWindow>();
    [Tooltip("An inactive button used as a model for the function buttons.")]
    [SerializeField] private LabeledButton functionButtonTemplate;
    [Tooltip("Where the function buttons are put (it should have a layout group).")]
    [SerializeField] private RectTransform functionButtonContainer;
    [SerializeField] private Button leaveButton;
    [Tooltip("Optional: the map's pause, held while the town is open. Without it the window sets the time scale itself.")]
    [SerializeField] private CampaignPause pause;

    private readonly System.Text.StringBuilder text = new System.Text.StringBuilder(256);
    private PointOfInterest town;
    private TownFunctionWindow openWindow;
    private bool isOpen;

    private void Start()
    {
        if (townRest == null || root == null || titleText == null || subtitleText == null || groupText == null
            || summaryText == null || party == null || capacity == null || wallet == null || inventory == null
            || functionButtonTemplate == null || functionButtonContainer == null || leaveButton == null)
        {
            Debug.LogError("TownPanel: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        functionButtonTemplate.gameObject.SetActive(false);
        for (int i = 0; i < functions.Count; i++)
        {
            TownFunctionWindow window = functions[i];
            if (window == null)
            {
                Debug.LogError("TownPanel: the function window " + i + " is not assigned.", this);
                continue;
            }

            LabeledButton button = Instantiate(functionButtonTemplate, functionButtonContainer);
            button.gameObject.SetActive(true);
            button.SetLabel(window.ButtonLabel);
            button.Button.onClick.AddListener(() => OpenWindow(window));
        }

        root.SetActive(false);
    }

    private void OnEnable()
    {
        if (townRest != null)
        {
            townRest.Rested += Open;
        }

        if (leaveButton != null)
        {
            leaveButton.onClick.AddListener(Close);
        }

        for (int i = 0; i < functions.Count; i++)
        {
            if (functions[i] != null)
            {
                functions[i].BackRequested += OnBackRequested;
            }
        }
    }

    private void OnDisable()
    {
        if (townRest != null)
        {
            townRest.Rested -= Open;
        }

        if (leaveButton != null)
        {
            leaveButton.onClick.RemoveListener(Close);
        }

        for (int i = 0; i < functions.Count; i++)
        {
            if (functions[i] != null)
            {
                functions[i].BackRequested -= OnBackRequested;
            }
        }
    }

    private void Open(PointOfInterest reached)
    {
        town = reached;
        isOpen = true;
        openWindow = null;
        Market market = Market.For(town);
        titleText.text = town.DisplayName;
        subtitleText.text = market != null ? "Ville " + market.Definition.ProfileName.ToLowerInvariant() : string.Empty;
        root.SetActive(true);
        UiLayering.BringToFront(root.transform);
        wallet.Changed += RefreshSummary;
        inventory.Changed += RefreshSummary;
        if (pause != null)
        {
            pause.SetHeld(this, true);
        }
        else
        {
            Time.timeScale = 0f;
        }

        Refresh();
    }

    private void Close()
    {
        if (!isOpen)
        {
            return;
        }

        isOpen = false;
        if (openWindow != null)
        {
            openWindow.Close();
            openWindow = null;
        }

        wallet.Changed -= RefreshSummary;
        inventory.Changed -= RefreshSummary;
        root.SetActive(false);
        if (pause != null)
        {
            pause.SetHeld(this, false);
        }
        else
        {
            Time.timeScale = 1f;
        }
    }

    private void OpenWindow(TownFunctionWindow window)
    {
        if (!isOpen || openWindow != null)
        {
            return;
        }

        openWindow = window;
        root.SetActive(false);
        window.Open(town);
    }

    private void OnBackRequested()
    {
        if (!isOpen || openWindow == null)
        {
            return;
        }

        openWindow.Close();
        openWindow = null;
        root.SetActive(true);
        UiLayering.BringToFront(root.transform);
        Refresh();
    }

    private void Refresh()
    {
        // The hospital is mentioned only when the group has lost hit points.
        text.Clear().Append(party.HasWounded ? "Vos troupes ont perdu des PV : passez à l'hôpital pour les soigner.\n\n" : string.Empty).Append("Groupe (")
            .Append(party.Members.Count).Append(" / ").Append(capacity.MaxSize).Append(") : ");
        for (int i = 0; i < party.Members.Count; i++)
        {
            PartyMember member = party.Members[i];
            if (i > 0)
            {
                text.Append(", ");
            }

            text.Append(member.name).Append(' ').Append(member.StartingHealth);
            if (member.definition != null)
            {
                text.Append('/').Append(member.definition.MaxHealth);
            }
        }

        groupText.text = text.ToString();
        RefreshSummary();
    }

    private void RefreshSummary()
    {
        text.Clear().Append(MoneyFormat.Unit).Append(" : ").Append(MoneyFormat.Number(wallet.Balance))
            .Append("\nCargaison : ").Append(inventory.TotalMass.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture))
            .Append(" / ").Append(inventory.Capacity.MaxMass.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)).Append(" kg, ")
            .Append(inventory.TotalVolume.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture))
            .Append(" / ").Append(inventory.Capacity.MaxVolume.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)).Append(" L");
        summaryText.text = text.ToString();
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The town window, opened when the caravan reaches a town (after its rest): the group and its coins, one button
/// per unit the town offers (grayed out with the reason when it cannot be recruited), and a button to leave.
/// The map is paused while the window is open, so no patrol reaches the caravan meanwhile. The recruiting itself
/// is TownRecruitment's business: this window only asks and shows.
/// </summary>
public class TownPanel : MonoBehaviour
{
    private class Row
    {
        public RecruitOffer Offer;
        public Button Button;
        public Text Label;
        public Text Reason;
    }

    [SerializeField] private TownRest townRest;
    [SerializeField] private TownRecruitment recruitment;
    [Tooltip("The whole window: hidden until the caravan reaches a town.")]
    [SerializeField] private GameObject root;
    [SerializeField] private Text titleText;
    [SerializeField] private Text groupText;
    [Tooltip("An inactive row used as a model: a Button with a child Label, and a child Reason text.")]
    [SerializeField] private Button offerTemplate;
    [Tooltip("Where the rows are put (it should have a vertical layout group).")]
    [SerializeField] private RectTransform offerContainer;
    [SerializeField] private Button leaveButton;

    private readonly List<Row> rows = new List<Row>();
    private readonly System.Text.StringBuilder text = new System.Text.StringBuilder(256);

    private void Start()
    {
        if (townRest == null || recruitment == null || root == null || titleText == null || groupText == null
            || offerTemplate == null || offerContainer == null || leaveButton == null)
        {
            Debug.LogError("TownPanel: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        offerTemplate.gameObject.SetActive(false);
        IReadOnlyList<RecruitOffer> offers = recruitment.Settings.Offers;
        for (int i = 0; i < offers.Count; i++)
        {
            Button button = Instantiate(offerTemplate, offerContainer);
            button.gameObject.SetActive(true);
            var row = new Row
            {
                Offer = offers[i],
                Button = button,
                Label = button.transform.Find("Label").GetComponent<Text>(),
                Reason = button.transform.Find("Reason").GetComponent<Text>()
            };
            RecruitOffer offer = offers[i];
            button.onClick.AddListener(() => OnRecruitClicked(offer));
            rows.Add(row);
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
    }

    private void Open(PointOfInterest town)
    {
        titleText.text = town.DisplayName;
        root.SetActive(true);
        Time.timeScale = 0f;
        Refresh();
    }

    private void Close()
    {
        root.SetActive(false);
        Time.timeScale = 1f;
    }

    private void OnRecruitClicked(RecruitOffer offer)
    {
        recruitment.Recruit(offer);
        Refresh();
    }

    private void Refresh()
    {
        PartyRoster party = recruitment.Party;
        text.Clear().Append("Votre groupe s'est reposé : tous ses PV sont rendus.\n\nGroupe (")
            .Append(party.Members.Count).Append(" / ").Append(recruitment.MaxSize).Append(") : ");
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

        text.Append("\nPièces : ").Append(party.Coins);
        groupText.text = text.ToString();

        for (int i = 0; i < rows.Count; i++)
        {
            Row row = rows[i];
            bool possible = recruitment.CanRecruit(row.Offer, out string reason);
            string unitName = row.Offer.definition != null ? row.Offer.definition.DisplayName : row.Offer.baseName;
            row.Label.text = "Recruter un " + unitName + "  -  " + row.Offer.price + " pièces";
            row.Reason.text = possible ? string.Empty : reason;
            row.Button.interactable = possible;
        }
    }
}

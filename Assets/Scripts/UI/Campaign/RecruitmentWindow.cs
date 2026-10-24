using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The recruitment window of a town: one button per unit the town offers (grayed out with the reason when it cannot
/// be recruited). The recruiting itself is TownRecruitment's business: this window only asks and shows.
/// </summary>
public class RecruitmentWindow : TownFunctionWindow
{
    private class Row
    {
        public RecruitOffer Offer;
        public Button Button;
        public Text Label;
        public Text Reason;
    }

    [SerializeField] private TownRecruitment recruitment;
    [SerializeField] private Text infoText;
    [Tooltip("An inactive row used as a model: a Button with a child Label, and a child Reason text.")]
    [SerializeField] private Button offerTemplate;
    [Tooltip("Where the rows are put (it should have a vertical layout group).")]
    [SerializeField] private RectTransform offerContainer;

    private readonly List<Row> rows = new List<Row>();
    private readonly System.Text.StringBuilder text = new System.Text.StringBuilder(128);

    protected override void Start()
    {
        base.Start();
        if (!enabled)
        {
            return;
        }

        if (recruitment == null || infoText == null || offerTemplate == null || offerContainer == null)
        {
            Debug.LogError("RecruitmentWindow: a serialized reference is not assigned.", this);
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
    }

    protected override void OnOpened(PointOfInterest town)
    {
        Refresh();
    }

    private void OnRecruitClicked(RecruitOffer offer)
    {
        recruitment.Recruit(offer);
        Refresh();
    }

    private void Refresh()
    {
        PartyRoster party = recruitment.Party;
        text.Clear().Append("Groupe (").Append(party.Members.Count).Append(" / ").Append(recruitment.MaxSize).Append(")     ")
            .Append(MoneyFormat.Unit).Append(" : ").Append(MoneyFormat.Number(recruitment.Wallet.Balance));
        infoText.text = text.ToString();

        for (int i = 0; i < rows.Count; i++)
        {
            Row row = rows[i];
            bool possible = recruitment.CanRecruit(row.Offer, out string reason);
            string unitName = row.Offer.definition != null ? row.Offer.definition.DisplayName : row.Offer.baseName;
            row.Label.text = "Recruter un " + unitName + "  -  " + MoneyFormat.WithUnit(row.Offer.price);
            row.Reason.text = possible ? string.Empty : reason;
            row.Button.interactable = possible;
        }
    }
}

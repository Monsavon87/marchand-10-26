using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The hospital window of a town: one line per unit of the group (its hit points and what healing it costs, with a
/// button grayed out and the reason written when it cannot be done) and a button for the whole group. The healing and
/// the payment are TownHospital's business: this window only asks and shows.
/// </summary>
public class HospitalWindow : TownFunctionWindow
{
    private class Row
    {
        public Button Button;
        public Text Label;
        public Text Reason;
    }

    [SerializeField] private TownHospital hospital;
    [SerializeField] private Text infoText;
    [Tooltip("An inactive row used as a model: a Button with a child Label, and a child Reason text.")]
    [SerializeField] private Button rowTemplate;
    [Tooltip("Where the rows are put (it should have a vertical layout group).")]
    [SerializeField] private RectTransform rowContainer;
    [SerializeField] private Button healAllButton;
    [SerializeField] private Text healAllReason;
    [SerializeField] private Text resultText;

    private readonly List<Row> rows = new List<Row>();
    private readonly System.Text.StringBuilder text = new System.Text.StringBuilder(160);

    protected override void Start()
    {
        base.Start();
        if (!enabled)
        {
            return;
        }

        if (hospital == null || infoText == null || rowTemplate == null || rowContainer == null || healAllButton == null
            || healAllReason == null || resultText == null)
        {
            Debug.LogError("HospitalWindow: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        rowTemplate.gameObject.SetActive(false);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        if (enabled && healAllButton != null)
        {
            healAllButton.onClick.AddListener(OnHealAll);
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (healAllButton != null)
        {
            healAllButton.onClick.RemoveListener(OnHealAll);
        }
    }

    protected override void OnOpened(PointOfInterest town)
    {
        resultText.text = string.Empty;
        hospital.Healed += OnHealed;
        hospital.Wallet.Changed += Refresh;
        Refresh();
    }

    protected override void OnClosing()
    {
        hospital.Healed -= OnHealed;
        hospital.Wallet.Changed -= Refresh;
    }

    private void OnHealed(int hitPoints, int price)
    {
        resultText.text = "Soigné : " + hitPoints + " PV rendus pour " + MoneyFormat.WithUnit(price);
        Refresh();
    }

    private void OnHealAll()
    {
        hospital.HealAll();
    }

    private void OnHealClicked(int index)
    {
        IReadOnlyList<PartyMember> members = hospital.Party.Members;
        if (index < members.Count)
        {
            hospital.Heal(members[index]);
        }
    }

    private void EnsureRows(int count)
    {
        while (rows.Count < count)
        {
            Button button = Instantiate(rowTemplate, rowContainer);
            button.gameObject.SetActive(true);
            int index = rows.Count;
            button.onClick.AddListener(() => OnHealClicked(index));
            rows.Add(new Row
            {
                Button = button,
                Label = button.transform.Find("Label").GetComponent<Text>(),
                Reason = button.transform.Find("Reason").GetComponent<Text>()
            });
        }

        // A unit that is gone (it died in a fight): its row goes too.
        while (rows.Count > count)
        {
            Destroy(rows[rows.Count - 1].Button.gameObject);
            rows.RemoveAt(rows.Count - 1);
        }
    }

    private void Refresh()
    {
        IReadOnlyList<PartyMember> members = hospital.Party.Members;
        text.Clear().Append(MoneyFormat.Unit).Append(" : ").Append(MoneyFormat.Number(hospital.Wallet.Balance))
            .Append("     Soin : ").Append(MoneyFormat.WithUnit(hospital.PricePerHitPoint)).Append(" par PV rendu");
        infoText.text = text.ToString();

        EnsureRows(members.Count);
        for (int i = 0; i < members.Count; i++)
        {
            PartyMember member = members[i];
            Row row = rows[i];
            bool possible = hospital.CanHeal(member, out string reason);
            text.Clear().Append(member.name).Append("  ").Append(member.StartingHealth).Append(" / ").Append(member.MaxHealth).Append(" PV");
            if (member.IsWounded)
            {
                text.Append("  -  Soigner pour ").Append(MoneyFormat.WithUnit(hospital.Price(member)));
            }

            row.Label.text = text.ToString();
            row.Reason.text = possible ? string.Empty : reason;
            row.Button.interactable = possible;
        }

        bool allPossible = hospital.CanHealAll(out string allReason);
        healAllButton.GetComponentInChildren<Text>().text = allPossible
            ? "Soigner tout le groupe  -  " + MoneyFormat.WithUnit(hospital.PriceAll())
            : "Soigner tout le groupe";
        healAllReason.text = allPossible ? string.Empty : allReason;
        healAllButton.interactable = allPossible;
    }
}

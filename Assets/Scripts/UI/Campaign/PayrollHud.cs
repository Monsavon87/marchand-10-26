using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One line of the map screen about what travelling costs: the daily cost of the group ("Salaires : 95 U.V. par jour",
/// with the first day it is due while the free days last) and, in red, the debt when there is one. It follows the
/// payroll, the group and the calendar through their events, so nothing runs each frame. Read-only.
/// </summary>
public class PayrollHud : MonoBehaviour
{
    [SerializeField] private PartyPayroll payroll;
    [SerializeField] private PartyRoster party;
    [SerializeField] private CampaignClock clock;
    [SerializeField] private Text label;
    [SerializeField] private Color debtColor = new Color(1f, 0.55f, 0.45f, 1f);

    private void Start()
    {
        if (payroll == null || party == null || clock == null || label == null)
        {
            Debug.LogError("PayrollHud: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        Refresh();
    }

    private void OnEnable()
    {
        if (payroll != null && party != null && clock != null)
        {
            payroll.Settled += OnSettled;
            payroll.DebtChanged += Refresh;
            party.MembersChanged += Refresh;
            clock.DayStarted += OnDayStarted;
        }
    }

    private void OnDisable()
    {
        if (payroll != null && party != null && clock != null)
        {
            payroll.Settled -= OnSettled;
            payroll.DebtChanged -= Refresh;
            party.MembersChanged -= Refresh;
            clock.DayStarted -= OnDayStarted;
        }
    }

    private void OnSettled(PayrollReport report)
    {
        Refresh();
    }

    private void OnDayStarted(int day)
    {
        Refresh();
    }

    private void Refresh()
    {
        int cost = payroll.DailyCost;
        string text = cost <= 0 ? "Salaires : aucun" : "Salaires : " + MoneyFormat.WithUnit(cost) + " par jour";
        if (cost > 0 && payroll.InGrace)
        {
            text += ", dès le jour " + (payroll.GraceDays + 1);
        }

        if (payroll.Debt > 0)
        {
            text += "     <color=#" + ColorUtility.ToHtmlStringRGB(debtColor) + ">Dette : " + MoneyFormat.WithUnit(payroll.Debt) + "</color>";
        }

        label.text = text;
    }
}

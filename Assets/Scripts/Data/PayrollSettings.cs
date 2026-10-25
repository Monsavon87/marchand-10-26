using UnityEngine;

/// <summary>
/// How the caravan's daily costs (the wages of its group first) are charged: the first days of the campaign are free,
/// so the player has the time to win a first loot before anything is due. What a unit earns per day is on its
/// UnitDefinition. Pure data.
/// </summary>
[CreateAssetMenu(fileName = "PayrollSettings", menuName = "Marchand/Payroll Settings")]
public class PayrollSettings : ScriptableObject
{
    [Tooltip("Days at the start of the campaign during which nothing is charged (day 1 to this day).")]
    [Min(0)] [SerializeField] private int graceDays = 7;
    [Tooltip("While a debt is owed, units cannot be recruited.")]
    [SerializeField] private bool debtBlocksRecruitment = true;

    public int GraceDays => graceDays;
    public bool DebtBlocksRecruitment => debtBlocksRecruitment;
}

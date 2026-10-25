/// <summary>The wages of the group: each unit costs its type's daily wage (UnitDefinition.DailyWage).</summary>
public class WageCost : IDailyCost
{
    public string Label => "Salaires";

    public int CostFor(int day, PartyRoster party)
    {
        long total = 0;
        for (int i = 0; i < party.Members.Count; i++)
        {
            total += party.Members[i].DailyWage;
        }

        return (int)System.Math.Min(total, int.MaxValue);
    }
}

using UnityEngine;

/// <summary>The goal of the demo: it lasts a number of world days, and the player's wealth at the end is the score. Pure data.</summary>
[CreateAssetMenu(fileName = "DemoGoalSettings", menuName = "Marchand/Demo Goal Settings")]
public class DemoGoalSettings : ScriptableObject
{
    [Tooltip("Days the demo lasts: it ends when the last of them is over.")]
    [Min(1)] [SerializeField] private int days = 100;
    [Tooltip("Count the goods in the cargo, at their base price, in the final wealth (otherwise only the money counts).")]
    [SerializeField] private bool countCargo = true;

    public int Days => days;
    public bool CountCargo => countCargo;
}

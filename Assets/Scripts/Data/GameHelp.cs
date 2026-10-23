using UnityEngine;

/// <summary>
/// The texts of the help page (principle of the game and controls), shown by the main menu and by the help
/// button in the game. One asset, so both places always say the same thing. Pure data.
/// </summary>
[CreateAssetMenu(fileName = "GameHelp", menuName = "Marchand/Game Help")]
public class GameHelp : ScriptableObject
{
    [SerializeField] private string title = "Principe et contrôles";
    [TextArea(4, 12)] [SerializeField] private string principle;
    [TextArea(4, 16)] [SerializeField] private string mapControls;
    [TextArea(4, 16)] [SerializeField] private string combatControls;

    public string Title => title;
    public string Principle => principle;
    public string MapControls => mapControls;
    public string CombatControls => combatControls;
}

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The help page: the principle of the game and the controls, read from the GameHelp asset. It can be shown,
/// hidden and closed with its own button. Used by the main menu and in the game.
/// </summary>
public class HelpPanel : MonoBehaviour
{
    [SerializeField] private GameHelp help;
    [Tooltip("The whole page: shown and hidden.")]
    [SerializeField] private GameObject root;
    [SerializeField] private Text titleText;
    [SerializeField] private Text principleText;
    [SerializeField] private Text mapControlsText;
    [SerializeField] private Text combatControlsText;
    [SerializeField] private Button closeButton;

    public bool IsOpen => root != null && root.activeSelf;

    private void Awake()
    {
        if (help == null || root == null || titleText == null || principleText == null || mapControlsText == null
            || combatControlsText == null || closeButton == null)
        {
            Debug.LogError("HelpPanel: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        titleText.text = help.Title;
        principleText.text = help.Principle;
        mapControlsText.text = help.MapControls;
        combatControlsText.text = help.CombatControls;
        root.SetActive(false);
    }

    private void OnEnable()
    {
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Hide);
        }
    }

    private void OnDisable()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Hide);
        }
    }

    public void Show()
    {
        root.SetActive(true);
    }

    public void Hide()
    {
        root.SetActive(false);
    }

    public void Toggle()
    {
        root.SetActive(!root.activeSelf);
    }
}

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The two buttons of the campaign map: Menu (back to the title screen) and Aide (opens or closes the help page).
/// </summary>
public class CampaignMenuButtons : MonoBehaviour
{
    [SerializeField] private Button menuButton;
    [SerializeField] private Button helpButton;
    [SerializeField] private HelpPanel helpPanel;
    [SerializeField] private string menuScene = "MainMenu";

    private void Start()
    {
        if (menuButton == null || helpButton == null || helpPanel == null)
        {
            Debug.LogError("CampaignMenuButtons: a serialized reference is not assigned.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (menuButton != null && helpButton != null)
        {
            menuButton.onClick.AddListener(BackToMenu);
            helpButton.onClick.AddListener(ToggleHelp);
        }
    }

    private void OnDisable()
    {
        if (menuButton != null && helpButton != null)
        {
            menuButton.onClick.RemoveListener(BackToMenu);
            helpButton.onClick.RemoveListener(ToggleHelp);
        }
    }

    private void BackToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuScene);
    }

    private void ToggleHelp()
    {
        helpPanel.Toggle();
    }
}

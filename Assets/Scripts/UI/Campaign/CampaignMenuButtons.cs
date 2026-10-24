using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The two buttons of the campaign map: Menu (back to the title screen) and Aide (opens or closes the help page).
/// The map is paused while the help page is open, however it gets closed.
/// </summary>
public class CampaignMenuButtons : MonoBehaviour
{
    [SerializeField] private Button menuButton;
    [SerializeField] private Button helpButton;
    [SerializeField] private HelpPanel helpPanel;
    [Tooltip("The map's pause, held while the help page is open.")]
    [SerializeField] private CampaignPause pause;
    [SerializeField] private string menuScene = "MainMenu";

    private void Start()
    {
        if (menuButton == null || helpButton == null || helpPanel == null || pause == null)
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

        if (helpPanel != null)
        {
            helpPanel.OpenChanged += OnHelpOpenChanged;
        }
    }

    private void OnDisable()
    {
        if (menuButton != null && helpButton != null)
        {
            menuButton.onClick.RemoveListener(BackToMenu);
            helpButton.onClick.RemoveListener(ToggleHelp);
        }

        if (helpPanel != null)
        {
            helpPanel.OpenChanged -= OnHelpOpenChanged;
        }

        // Disabled with the page still open: do not leave the map frozen.
        if (pause != null)
        {
            pause.SetHeld(this, false);
        }
    }

    private void OnHelpOpenChanged(bool open)
    {
        pause.SetHeld(this, open);
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

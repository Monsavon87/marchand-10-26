using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The title screen: Play starts the campaign, "Principe et contrôles" opens the help page, Quit leaves the game.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button helpButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private HelpPanel helpPanel;
    [Tooltip("The campaign scene Play loads (it must be in the build settings).")]
    [SerializeField] private string campaignScene = "Demo_Campaign";

    private void Start()
    {
        if (playButton == null || helpButton == null || quitButton == null || helpPanel == null)
        {
            Debug.LogError("MainMenu: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        Time.timeScale = 1f;
    }

    private void OnEnable()
    {
        if (playButton != null && helpButton != null && quitButton != null)
        {
            playButton.onClick.AddListener(Play);
            helpButton.onClick.AddListener(ShowHelp);
            quitButton.onClick.AddListener(Quit);
        }
    }

    private void OnDisable()
    {
        if (playButton != null && helpButton != null && quitButton != null)
        {
            playButton.onClick.RemoveListener(Play);
            helpButton.onClick.RemoveListener(ShowHelp);
            quitButton.onClick.RemoveListener(Quit);
        }
    }

    private void Play()
    {
        SceneManager.LoadScene(campaignScene);
    }

    private void ShowHelp()
    {
        helpPanel.Show();
    }

    private void Quit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}

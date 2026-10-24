using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The "Fin de partie" screen of the campaign map: it appears when the caravan lost a fight; one button starts the
/// campaign again (the map scene is reloaded), the other goes back to the title screen. Provisional, until defeats
/// have real consequences.
/// </summary>
public class GameOverScreen : MonoBehaviour
{
    [SerializeField] private CampaignEncounterFlow flow;
    [Tooltip("The whole screen: hidden until the game is over.")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button restartButton;
    [Tooltip("Optional: back to the title screen.")]
    [SerializeField] private Button menuButton;
    [SerializeField] private string menuScene = "MainMenu";

    private void Start()
    {
        if (flow == null || root == null || restartButton == null)
        {
            Debug.LogError("GameOverScreen: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        root.SetActive(false);
    }

    private void OnEnable()
    {
        if (flow != null)
        {
            flow.GameOver += OnGameOver;
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(Restart);
        }

        if (menuButton != null)
        {
            menuButton.onClick.AddListener(BackToMenu);
        }
    }

    private void OnDisable()
    {
        if (flow != null)
        {
            flow.GameOver -= OnGameOver;
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(Restart);
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveListener(BackToMenu);
        }
    }

    private void OnGameOver()
    {
        root.SetActive(true);
        UiLayering.BringToFront(root.transform);
    }

    private void BackToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuScene);
    }

    private void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameObject.scene.name);
    }
}

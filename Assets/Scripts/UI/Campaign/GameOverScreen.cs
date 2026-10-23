using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The "Fin de partie" screen of the campaign map: it appears when the caravan lost a fight, and its button starts
/// the campaign again (the map scene is reloaded). Provisional, until defeats have real consequences.
/// </summary>
public class GameOverScreen : MonoBehaviour
{
    [SerializeField] private CampaignEncounterFlow flow;
    [Tooltip("The whole screen: hidden until the game is over.")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button restartButton;

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
    }

    private void OnGameOver()
    {
        root.SetActive(true);
    }

    private void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameObject.scene.name);
    }
}

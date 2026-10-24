using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The end of the demo: when the last of its days is over (see DemoGoal), "Démo terminée" with the final wealth (the
/// money and the cargo at base price, the score), the loot gathered and the bands beaten, and a button back to the
/// title screen.
/// </summary>
public class DemoEndScreen : MonoBehaviour
{
    [SerializeField] private DemoGoal goal;
    [SerializeField] private PartyRoster party;
    [Tooltip("Optional: gives the number of bands beaten.")]
    [SerializeField] private BandRespawner respawner;
    [Tooltip("The whole screen: hidden until the demo is over.")]
    [SerializeField] private GameObject root;
    [SerializeField] private Text detailsText;
    [SerializeField] private Button menuButton;
    [SerializeField] private string menuScene = "MainMenu";

    private void Start()
    {
        if (goal == null || party == null || root == null || detailsText == null || menuButton == null)
        {
            Debug.LogError("DemoEndScreen: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        root.SetActive(false);
    }

    private void OnEnable()
    {
        if (goal != null)
        {
            goal.Finished += OnFinished;
        }

        if (menuButton != null)
        {
            menuButton.onClick.AddListener(BackToMenu);
        }
    }

    private void OnDisable()
    {
        if (goal != null)
        {
            goal.Finished -= OnFinished;
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveListener(BackToMenu);
        }
    }

    private void OnFinished(DemoScore score)
    {
        string text = "Les " + goal.Days + " jours sont écoulés.\n\nRichesse finale : " + MoneyFormat.WithUnit((int)System.Math.Min(score.Total, int.MaxValue))
            + "\n(argent : " + MoneyFormat.WithUnit(score.Money)
            + (score.CargoCounted ? ", cargaison : " + MoneyFormat.WithUnit((int)System.Math.Min(score.CargoValue, int.MaxValue)) : string.Empty) + ")"
            + "\nValeur du butin ramassé : " + MoneyFormat.WithUnit(party.Gathered);
        if (respawner != null)
        {
            text += "\nBandes battues : " + respawner.TotalDefeated;
        }

        detailsText.text = text + "\n\nMerci d'avoir joué !";
        root.SetActive(true);
        UiLayering.BringToFront(root.transform);
    }

    private void BackToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuScene);
    }
}

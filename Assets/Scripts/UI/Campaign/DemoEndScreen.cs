using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The end of the demo: when every enemy band of the map is beaten, "Démo terminée" with the total loot gathered and a
/// button back to the title screen.
/// </summary>
public class DemoEndScreen : MonoBehaviour
{
    [SerializeField] private CampaignEncounterFlow flow;
    [SerializeField] private PartyRoster party;
    [Tooltip("The whole screen: hidden until the demo is over.")]
    [SerializeField] private GameObject root;
    [SerializeField] private Text detailsText;
    [SerializeField] private Button menuButton;
    [SerializeField] private string menuScene = "MainMenu";

    private void Start()
    {
        if (flow == null || party == null || root == null || detailsText == null || menuButton == null)
        {
            Debug.LogError("DemoEndScreen: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        root.SetActive(false);
    }

    private void OnEnable()
    {
        if (flow != null)
        {
            flow.AllBandsDefeated += OnAllBandsDefeated;
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
            flow.AllBandsDefeated -= OnAllBandsDefeated;
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveListener(BackToMenu);
        }
    }

    private void OnAllBandsDefeated()
    {
        detailsText.text = "Toutes les bandes de brigands sont vaincues.\nButin total : " + MoneyFormat.WithUnit(party.Gathered)
            + "\n\nMerci d'avoir joué !";
        root.SetActive(true);
        UiLayering.BringToFront(root.transform);
    }

    private void BackToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(menuScene);
    }
}

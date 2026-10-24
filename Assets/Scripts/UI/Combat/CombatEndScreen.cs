using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The end-of-combat screen: "Victoire", "Défaite" or "Match nul", the losses of both sides and the loot, with a
/// Continue button. It appears when the combat manager says the combat ended. When the fight came from the campaign
/// map, Continue asks to go back there (through the encounter channel). Otherwise (a test arena) it loads the scene
/// named in 'nextScene', or reloads the current scene when it is empty.
/// </summary>
public class CombatEndScreen : MonoBehaviour
{
    [SerializeField] private CombatManager combat;
    [Tooltip("The whole screen: hidden until the combat ends.")]
    [SerializeField] private GameObject root;
    [SerializeField] private Text titleText;
    [SerializeField] private Button continueButton;
    [Tooltip("Optional: the losses and the loot. Needs the encounter channel.")]
    [SerializeField] private Text detailsText;
    [Tooltip("Optional: the link with the campaign map.")]
    [SerializeField] private EncounterChannel channel;
    [Tooltip("Scene to load when Continue is pressed. Empty = reload this scene.")]
    [SerializeField] private string nextScene;
    [SerializeField] private Color victoryColor = new Color(0.55f, 0.95f, 0.55f);
    [SerializeField] private Color defeatColor = new Color(1f, 0.45f, 0.4f);
    [SerializeField] private Color drawColor = new Color(0.9f, 0.9f, 0.9f);

    private bool detailsShown;

    private void Start()
    {
        if (combat == null || root == null || titleText == null || continueButton == null)
        {
            Debug.LogError("CombatEndScreen: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        root.SetActive(false);
        if (detailsText != null)
        {
            detailsText.text = string.Empty;
        }
    }

    // The result of the encounter is written by whoever listens to the same end of combat; it may come after us.
    private void Update()
    {
        if (detailsShown || detailsText == null || channel == null || !root.activeSelf)
        {
            return;
        }

        EncounterData encounter = channel.Current;
        if (encounter == null || !encounter.HasResult)
        {
            return;
        }

        detailsShown = true;
        string text = "Vos pertes : " + (encounter.PlayerLosses.Count == 0 ? "aucune" : string.Join(", ", encounter.PlayerLosses))
            + "\nPertes ennemies : " + (encounter.EnemyLosses.Count == 0 ? "aucune" : string.Join(", ", encounter.EnemyLosses));
        if (encounter.Winner == Team.Player && encounter.Loot > 0)
        {
            text += "\nButin : " + MoneyFormat.WithUnit(encounter.Loot);
        }

        detailsText.text = text;
    }

    private void OnEnable()
    {
        if (combat != null)
        {
            combat.CombatEnded += OnCombatEnded;
        }

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(Continue);
        }
    }

    private void OnDisable()
    {
        if (combat != null)
        {
            combat.CombatEnded -= OnCombatEnded;
        }

        if (continueButton != null)
        {
            continueButton.onClick.RemoveListener(Continue);
        }
    }

    private void OnCombatEnded(Team? winner)
    {
        root.SetActive(true);
        if (!winner.HasValue)
        {
            titleText.text = "Match nul";
            titleText.color = drawColor;
        }
        else if (winner.Value == Team.Player)
        {
            titleText.text = "Victoire !";
            titleText.color = victoryColor;
        }
        else
        {
            titleText.text = "Défaite";
            titleText.color = defeatColor;
        }
    }

    private void Continue()
    {
        Time.timeScale = 1f;
        if (channel != null && channel.Current != null && channel.Current.ReturnToMap)
        {
            continueButton.interactable = false;
            channel.RequestReturn();
            return;
        }

        string target = string.IsNullOrEmpty(nextScene) ? gameObject.scene.name : nextScene;
#if UNITY_EDITOR
        // The prototype's scenes are not in the build settings: load by path, which works in the editor.
        string path = string.IsNullOrEmpty(nextScene) ? gameObject.scene.path : FindScenePath(nextScene);
        if (!string.IsNullOrEmpty(path))
        {
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
            return;
        }
#endif
        SceneManager.LoadScene(target);
    }

#if UNITY_EDITOR
    private static string FindScenePath(string sceneName)
    {
        string[] guids = UnityEditor.AssetDatabase.FindAssets(sceneName + " t:Scene");
        for (int i = 0; i < guids.Length; i++)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName)
            {
                return path;
            }
        }

        return null;
    }
#endif
}

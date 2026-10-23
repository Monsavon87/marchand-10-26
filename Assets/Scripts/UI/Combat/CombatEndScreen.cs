using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// The end-of-combat screen: "Victoire", "Défaite" or "Match nul", with a Continue button. It appears when the
/// combat manager says the combat ended. Continue loads the scene named in 'nextScene', or reloads the current
/// scene when it is empty (until the campaign is linked to the combat). Loading a scene by name needs it in the
/// build settings; in the editor the current scene is reloaded directly.
/// </summary>
public class CombatEndScreen : MonoBehaviour
{
    [SerializeField] private CombatManager combat;
    [Tooltip("The whole screen: hidden until the combat ends.")]
    [SerializeField] private GameObject root;
    [SerializeField] private Text titleText;
    [SerializeField] private Button continueButton;
    [Tooltip("Scene to load when Continue is pressed. Empty = reload this scene.")]
    [SerializeField] private string nextScene;
    [SerializeField] private Color victoryColor = new Color(0.55f, 0.95f, 0.55f);
    [SerializeField] private Color defeatColor = new Color(1f, 0.45f, 0.4f);
    [SerializeField] private Color drawColor = new Color(0.9f, 0.9f, 0.9f);

    private void Start()
    {
        if (combat == null || root == null || titleText == null || continueButton == null)
        {
            Debug.LogError("CombatEndScreen: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        root.SetActive(false);
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

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the game's version (the one set in the player settings) on screen, so a tester can say which version
/// a bug or a remark comes from.
/// </summary>
public class VersionLabel : MonoBehaviour
{
    [SerializeField] private Text label;
    [SerializeField] private string prefix = "Version ";

    private void Start()
    {
        if (label == null)
        {
            Debug.LogError("VersionLabel: label is not assigned.", this);
            enabled = false;
            return;
        }

        label.text = prefix + Application.version;
    }
}

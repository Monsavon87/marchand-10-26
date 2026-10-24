using UnityEngine;
using UnityEngine.UI;

/// <summary>A button and the text written on it, kept together so a script can build many of them from one model.</summary>
public class LabeledButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Text label;

    public Button Button => button;

    public void SetLabel(string text)
    {
        if (label == null)
        {
            Debug.LogError("LabeledButton: the label is not assigned.", this);
            return;
        }

        label.text = text;
    }
}

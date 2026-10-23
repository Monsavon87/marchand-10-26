using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The order bar of the selected unit: one chip per order, the action in progress first (highlighted), then the
/// orders lined up behind it. It only reads the unit's orders. The chips are a fixed set of pre-made objects: this
/// script shows or hides them and sets their text.
/// </summary>
public class OrderBar : MonoBehaviour
{
    [SerializeField] private SelectionManager selection;
    [Tooltip("The whole bar: hidden while nothing is selected.")]
    [SerializeField] private GameObject root;
    [Tooltip("One entry per chip, in order. At least as many as the longest queue plus the current action.")]
    [SerializeField] private Image[] chipBackgrounds;
    [SerializeField] private Text[] chipLabels;
    [SerializeField] private Color currentColor = new Color(0.2f, 0.55f, 0.9f, 0.95f);
    [SerializeField] private Color queuedColor = new Color(0.25f, 0.25f, 0.3f, 0.9f);
    [SerializeField] private Color idleColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    [Min(0.02f)] [SerializeField] private float refreshInterval = 0.1f;

    private readonly List<string> labels = new List<string>(8);
    private float nextRefresh;

    private void Start()
    {
        if (selection == null || root == null || chipBackgrounds == null || chipLabels == null
            || chipBackgrounds.Length == 0 || chipBackgrounds.Length != chipLabels.Length)
        {
            Debug.LogError("OrderBar: a serialized reference is not assigned, or the chips do not match.", this);
            enabled = false;
            return;
        }

        Refresh();
    }

    private void OnEnable()
    {
        if (selection != null)
        {
            selection.SelectionChanged += Refresh;
        }
    }

    private void OnDisable()
    {
        if (selection != null)
        {
            selection.SelectionChanged -= Refresh;
        }
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh)
        {
            return;
        }

        nextRefresh = Time.unscaledTime + refreshInterval;
        Refresh();
    }

    private void Refresh()
    {
        ActionController actions = selection.Selected.Count > 0 ? selection.Selected[0].GetComponent<ActionController>() : null;
        if (actions == null)
        {
            root.SetActive(false);
            return;
        }

        root.SetActive(true);
        int count = actions.GetOrderLabels(labels);
        for (int i = 0; i < chipLabels.Length; i++)
        {
            bool shown = i < count || (i == 0 && count == 0);
            chipBackgrounds[i].gameObject.SetActive(shown);
            if (!shown)
            {
                continue;
            }

            if (count == 0)
            {
                chipLabels[i].text = "Inactif";
                chipBackgrounds[i].color = idleColor;
            }
            else
            {
                chipLabels[i].text = labels[i];
                chipBackgrounds[i].color = i == 0 ? currentColor : queuedColor;
            }
        }
    }
}

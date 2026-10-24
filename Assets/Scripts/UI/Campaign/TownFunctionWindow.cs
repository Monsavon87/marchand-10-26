using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// What every window of a town function (the market, the recruitment, later others) has in common: it opens for a
/// town, closes, and asks to go back to the town's main window with its own button. The main window (TownPanel)
/// builds one button per window it is given and shows one window at a time. The script must sit on an object that
/// stays active: 'root' is the part that is shown and hidden.
/// </summary>
public abstract class TownFunctionWindow : MonoBehaviour
{
    [Tooltip("The whole window, with its dimmed background: hidden until the town's main window opens it.")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button backButton;
    [Tooltip("What the button of this window says in the town's main window.")]
    [SerializeField] private string buttonLabel = "Fonction";

    /// <summary>Raised when the player asks to go back to the town's main window.</summary>
    public event Action BackRequested = delegate { };

    public string ButtonLabel => buttonLabel;
    public bool IsOpen => root != null && root.activeSelf;

    protected virtual void Start()
    {
        if (root == null || backButton == null)
        {
            Debug.LogError(GetType().Name + ": the window root or its back button is not assigned.", this);
            enabled = false;
            return;
        }

        root.SetActive(false);
    }

    protected virtual void OnEnable()
    {
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClicked);
        }
    }

    protected virtual void OnDisable()
    {
        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnBackClicked);
        }
    }

    /// <summary>Shows the window for this town.</summary>
    public void Open(PointOfInterest town)
    {
        root.SetActive(true);
        UiLayering.BringToFront(root.transform);
        OnOpened(town);
    }

    /// <summary>Hides the window.</summary>
    public void Close()
    {
        OnClosing();
        root.SetActive(false);
    }

    protected abstract void OnOpened(PointOfInterest town);

    protected virtual void OnClosing()
    {
    }

    private void OnBackClicked()
    {
        BackRequested();
    }
}

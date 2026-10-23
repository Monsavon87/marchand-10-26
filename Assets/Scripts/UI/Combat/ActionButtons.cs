using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The action buttons of the combat interface and their keyboard shortcuts: Attack (choose a target), Wait, Cancel,
/// Pause. Each one only asks: the attack button puts the command controller in targeting mode, the wait and cancel
/// buttons ask the selected units' ActionController (like any order), the pause button asks the combat manager.
/// The cancel key itself (X) and the pause key (Space) belong to the command controller and the combat manager;
/// the buttons show them. Nothing here decides what an action does.
/// </summary>
public class ActionButtons : MonoBehaviour
{
    [SerializeField] private SelectionManager selection;
    [SerializeField] private PlayerCommandController commands;
    [SerializeField] private CombatManager combat;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button waitButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button pauseButton;
    [Tooltip("The attack button's background: colored while the unit is waiting for a target.")]
    [SerializeField] private Image attackBackground;
    [SerializeField] private Text pauseLabel;
    [Tooltip("Keys are physical positions (US layout). A is taken by the camera, so the attack key is C.")]
    [SerializeField] private Key attackKey = Key.C;
    [SerializeField] private Key waitKey = Key.T;
    [Tooltip("How long, in seconds, the Wait button makes the selected units wait.")]
    [Min(0.1f)] [SerializeField] private float waitSeconds = 2f;
    [SerializeField] private Color armedColor = new Color(0.9f, 0.55f, 0.15f, 1f);
    [SerializeField] private Color normalColor = Color.white;

    private void Start()
    {
        if (selection == null || commands == null || combat == null || attackButton == null || waitButton == null
            || cancelButton == null || pauseButton == null || attackBackground == null || pauseLabel == null)
        {
            Debug.LogError("ActionButtons: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        attackBackground.color = normalColor;
    }

    private void OnEnable()
    {
        if (!enabled || attackButton == null)
        {
            return;
        }

        attackButton.onClick.AddListener(ToggleTargeting);
        waitButton.onClick.AddListener(WaitSelected);
        cancelButton.onClick.AddListener(CancelSelected);
        pauseButton.onClick.AddListener(TogglePause);
        commands.TargetingModeChanged += OnTargetingModeChanged;
    }

    private void OnDisable()
    {
        if (attackButton == null)
        {
            return;
        }

        attackButton.onClick.RemoveListener(ToggleTargeting);
        waitButton.onClick.RemoveListener(WaitSelected);
        cancelButton.onClick.RemoveListener(CancelSelected);
        pauseButton.onClick.RemoveListener(TogglePause);
        if (commands != null)
        {
            commands.TargetingModeChanged -= OnTargetingModeChanged;
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            if (keyboard[attackKey].wasPressedThisFrame)
            {
                ToggleTargeting();
            }

            if (keyboard[waitKey].wasPressedThisFrame)
            {
                WaitSelected();
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                commands.TargetingMode = false;
            }
        }

        // The buttons that act on units are only usable with a unit selected and a combat going on.
        bool running = combat.State == CombatState.Running;
        bool hasSelection = selection.Selected.Count > 0;
        attackButton.interactable = running && hasSelection;
        waitButton.interactable = running && hasSelection;
        cancelButton.interactable = running && hasSelection;
        pauseButton.interactable = running;
        pauseLabel.text = combat.IsPaused ? "Reprendre (Espace)" : "Pause (Espace)";

        // Targeting ends when nothing is selected any more.
        if (!hasSelection && commands.TargetingMode)
        {
            commands.TargetingMode = false;
        }
    }

    private void ToggleTargeting()
    {
        if (combat.State != CombatState.Running)
        {
            return;
        }

        if (selection.Selected.Count == 0)
        {
            CombatEvents.RaiseOrderRefused(Team.Player, string.Empty, "sélectionnez d'abord une unité");
            return;
        }

        commands.TargetingMode = !commands.TargetingMode;
    }

    private void OnTargetingModeChanged(bool armed)
    {
        attackBackground.color = armed ? armedColor : normalColor;
        if (armed)
        {
            CombatEvents.RaiseOrderRefused(Team.Player, string.Empty, "cliquez sur un ennemi à attaquer (Échap pour annuler)");
        }
    }

    private void WaitSelected()
    {
        if (combat.State != CombatState.Running)
        {
            return;
        }

        System.Collections.Generic.IReadOnlyList<SelectableUnit> units = selection.Selected;
        for (int i = 0; i < units.Count; i++)
        {
            ActionController actions = units[i].GetComponent<ActionController>();
            if (actions != null)
            {
                actions.RequestWait(waitSeconds);
            }
        }
    }

    private void CancelSelected()
    {
        System.Collections.Generic.IReadOnlyList<SelectableUnit> units = selection.Selected;
        for (int i = 0; i < units.Count; i++)
        {
            ActionController actions = units[i].GetComponent<ActionController>();
            if (actions != null)
            {
                actions.Cancel();
            }
        }
    }

    private void TogglePause()
    {
        combat.SetPaused(!combat.IsPaused);
    }
}

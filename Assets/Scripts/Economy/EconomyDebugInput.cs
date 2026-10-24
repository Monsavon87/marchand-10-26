using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Debug only: a key moves the calendar forward by one day, with Shift by a week, through the clock itself, so the
/// markets live each day one by one exactly as if the caravan had walked there. Works even in a frozen world. Remove
/// or disable it once the time passing in town exists.
/// </summary>
public class EconomyDebugInput : MonoBehaviour
{
    [SerializeField] private CampaignClock clock;
    [Tooltip("Keys are physical positions (US layout).")]
    [SerializeField] private Key advanceKey = Key.F5;
    [Tooltip("Days jumped with Shift held.")]
    [Min(2)] [SerializeField] private int shiftDays = 7;

    private void Start()
    {
        if (clock == null)
        {
            Debug.LogError("EconomyDebugInput: the clock is not assigned.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard[advanceKey].wasPressedThisFrame)
        {
            return;
        }

        bool shift = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
        int days = shift ? shiftDays : 1;
        clock.AdvanceMinutes(days * 1440f);
        Debug.Log("[Debug économie] calendrier avancé de " + days + " jour(s) : jour " + clock.Day);
    }
}

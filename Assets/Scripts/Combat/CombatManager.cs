using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Real-time combat with pause: it owns the time mode (Space pauses the game, orders can still be given).
/// There are no rounds: what an action costs in time is the action's own business (walking takes the time it
/// takes, an attack lasts its attack time). A future initiative system will decide who acts when.
/// </summary>
public class CombatManager : MonoBehaviour
{
    [SerializeField] private Key pauseKey = Key.Space;

    public bool IsPaused { get; private set; }

    /// <summary>Raised when the game is paused (true) or resumed (false).</summary>
    public event Action<bool> PauseChanged = delegate { };

    private void OnEnable()
    {
        Time.timeScale = 1f;
    }

    private void OnDisable()
    {
        // Never leave the game frozen.
        Time.timeScale = 1f;
        IsPaused = false;
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard[pauseKey].wasPressedThisFrame)
        {
            SetPaused(!IsPaused);
        }
    }

    /// <summary>Pauses or resumes the game.</summary>
    public void SetPaused(bool paused)
    {
        if (IsPaused == paused)
        {
            return;
        }

        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        PauseChanged(paused);
    }
}

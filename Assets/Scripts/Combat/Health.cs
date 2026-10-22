using System;
using UnityEngine;

/// <summary>
/// Hit points of a unit. It takes final damage (armor is the combat rules' business, not its own),
/// can be healed, and raises an event when damaged and when it dies. It does not decide what dying means:
/// whoever listens to Died does (see UnitDeath).
/// </summary>
public class Health : MonoBehaviour
{
    [SerializeField] private UnitStats stats;

    private bool initialized;

    /// <summary>Hit points left.</summary>
    public int Current { get; private set; }
    public int Max => stats.MaxHealth;
    public bool IsDead { get; private set; }

    /// <summary>Raised after damage was taken: (amount really lost, hit points left). Also raised for the killing blow, just before Died.</summary>
    public event Action<int, int> Damaged = delegate { };
    /// <summary>Raised after healing: (amount really restored, hit points left).</summary>
    public event Action<int, int> Healed = delegate { };
    /// <summary>Raised once, when the hit points reach 0.</summary>
    public event Action Died = delegate { };

    private void Start()
    {
        Initialize();
    }

    /// <summary>Takes damage. Zero or negative amounts and dead units are ignored.</summary>
    public void TakeDamage(int amount)
    {
        Initialize();
        if (IsDead || amount <= 0)
        {
            return;
        }

        int lost = Mathf.Min(amount, Current);
        Current -= lost;
        Damaged(lost, Current);

        if (Current <= 0)
        {
            IsDead = true;
            Died();
        }
    }

    /// <summary>Restores hit points up to the maximum. Dead units cannot be healed.</summary>
    public void Heal(int amount)
    {
        Initialize();
        if (IsDead || amount <= 0)
        {
            return;
        }

        int restored = Mathf.Min(amount, Max - Current);
        if (restored <= 0)
        {
            return;
        }

        Current += restored;
        Healed(restored, Current);
    }

    private void Initialize()
    {
        if (initialized)
        {
            return;
        }

        if (stats == null)
        {
            Debug.LogError("Health: stats is not assigned.", this);
            enabled = false;
            return;
        }

        initialized = true;
        Current = stats.MaxHealth;
    }
}

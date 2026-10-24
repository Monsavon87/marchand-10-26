using System;
using UnityEngine;

/// <summary>
/// The one source of truth for the caravan's money, in U.V. (the game's only currency), always a whole number.
/// TryDeposit and TryWithdraw are the only ways to change the balance, and each is all-or-nothing: a refused
/// operation changes nothing and says why. Pure data plus its rules; the texts shown to the player are made by
/// whoever asks.
/// </summary>
public class Wallet : MonoBehaviour
{
    [Tooltip("U.V. the caravan owns when the game starts.")]
    [Min(0)] [SerializeField] private int startingBalance;

    /// <summary>Raised after the balance changed (a deposit or a withdrawal that was accepted).</summary>
    public event Action Changed = delegate { };

    public int Balance { get; private set; }

    private void Awake()
    {
        Balance = Mathf.Max(0, startingBalance);
    }

    /// <summary>True when the balance covers this amount (an amount of 0 or less is always covered).</summary>
    public bool CanAfford(int amount)
    {
        return amount <= Balance;
    }

    /// <summary>Would depositing this amount be accepted, and if not why? Changes nothing.</summary>
    public WalletResult CheckDeposit(int amount)
    {
        if (amount <= 0)
        {
            return WalletResult.InvalidAmount;
        }

        return amount > int.MaxValue - Balance ? WalletResult.Overflow : WalletResult.Accepted;
    }

    /// <summary>Adds money. Accepted, or the reason why nothing was added.</summary>
    public WalletResult TryDeposit(int amount)
    {
        WalletResult check = CheckDeposit(amount);
        if (check != WalletResult.Accepted)
        {
            return check;
        }

        Balance += amount;
        Changed();
        return WalletResult.Accepted;
    }

    /// <summary>Takes money. Accepted, or the reason why nothing was taken.</summary>
    public WalletResult TryWithdraw(int amount)
    {
        if (amount <= 0)
        {
            return WalletResult.InvalidAmount;
        }

        if (amount > Balance)
        {
            return WalletResult.InsufficientFunds;
        }

        Balance -= amount;
        Changed();
        return WalletResult.Accepted;
    }
}

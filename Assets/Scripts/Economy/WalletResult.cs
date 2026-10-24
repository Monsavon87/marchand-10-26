/// <summary>The outcome of an operation on a Wallet. Anything but Accepted means the balance did not change.</summary>
public enum WalletResult
{
    Accepted,
    /// <summary>The amount is 0 or negative.</summary>
    InvalidAmount,
    /// <summary>Withdrawing more than the balance.</summary>
    InsufficientFunds,
    /// <summary>Depositing would go past the largest whole number the balance can hold.</summary>
    Overflow
}

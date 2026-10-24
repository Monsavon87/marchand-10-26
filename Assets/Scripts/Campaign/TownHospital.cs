using System;
using UnityEngine;

/// <summary>
/// The hospital of a town: it gives hit points back to the caravan's group for U.V., at a price per hit point given
/// back (see HealingSettings). One unit, or the whole group. Everything is checked before anything changes, then the
/// money goes through the wallet and the hit points come back. It says why when a healing is not possible, in the words
/// shown to the player.
/// </summary>
public class TownHospital : MonoBehaviour
{
    [SerializeField] private PartyRoster party;
    [SerializeField] private Wallet wallet;
    [SerializeField] private HealingSettings settings;

    /// <summary>Raised after a healing went through, with the hit points given back and what they cost.</summary>
    public event Action<int, int> Healed = delegate { };

    public PartyRoster Party => party;
    public Wallet Wallet => wallet;
    public int PricePerHitPoint => settings != null ? settings.PricePerHitPoint : 0;

    private void Start()
    {
        if (party == null || wallet == null || settings == null)
        {
            Debug.LogError("TownHospital: a serialized reference is not assigned.", this);
            enabled = false;
        }
    }

    /// <summary>What it costs to heal this unit completely, in U.V.</summary>
    public int Price(PartyMember member)
    {
        return (int)Math.Min(HealingRules.Price(HealingRules.Missing(member.StartingHealth, member.MaxHealth), settings.PricePerHitPoint), int.MaxValue);
    }

    /// <summary>What it costs to heal the whole group completely, in U.V.</summary>
    public int PriceAll()
    {
        long total = 0;
        for (int i = 0; i < party.Members.Count; i++)
        {
            total += Price(party.Members[i]);
        }

        return (int)Math.Min(total, int.MaxValue);
    }

    /// <summary>Can this unit be healed now? If not, 'reason' says why. Changes nothing.</summary>
    public bool CanHeal(PartyMember member, out string reason)
    {
        if (member == null || !member.IsWounded)
        {
            reason = "Déjà en pleine santé.";
            return false;
        }

        return CanPay(Price(member), out reason);
    }

    /// <summary>Can the whole group be healed now? If not, 'reason' says why. Changes nothing.</summary>
    public bool CanHealAll(out string reason)
    {
        if (!party.HasWounded)
        {
            reason = "Plus personne à soigner.";
            return false;
        }

        return CanPay(PriceAll(), out reason);
    }

    /// <summary>Heals this unit completely, for its price. False (and nothing changes) when it is not possible.</summary>
    public bool Heal(PartyMember member)
    {
        if (!CanHeal(member, out string reason))
        {
            Debug.Log("[Hôpital] refusé : " + reason, this);
            return false;
        }

        return Apply(member, Price(member));
    }

    /// <summary>Heals the whole group completely, for its total price. False (and nothing changes) when it is not possible.</summary>
    public bool HealAll()
    {
        if (!CanHealAll(out string reason))
        {
            Debug.Log("[Hôpital] refusé : " + reason, this);
            return false;
        }

        int price = PriceAll();
        if (!Pay(price))
        {
            return false;
        }

        int given = 0;
        for (int i = 0; i < party.Members.Count; i++)
        {
            given += Restore(party.Members[i]);
        }

        Healed(given, price);
        return true;
    }

    private bool Apply(PartyMember member, int price)
    {
        if (!Pay(price))
        {
            return false;
        }

        Healed(Restore(member), price);
        return true;
    }

    // The hit points come back after the payment; returns how many.
    private static int Restore(PartyMember member)
    {
        int before = member.StartingHealth;
        member.SetHealth(member.MaxHealth);
        return member.StartingHealth - before;
    }

    private bool Pay(int price)
    {
        // A free healing (price 0) needs no withdrawal.
        if (price <= 0)
        {
            return true;
        }

        WalletResult payment = wallet.TryWithdraw(price);
        if (payment != WalletResult.Accepted)
        {
            Debug.LogError("TownHospital: the payment was refused (" + payment + ") although the price was checked.", this);
            return false;
        }

        return true;
    }

    private bool CanPay(int price, out string reason)
    {
        if (!wallet.CanAfford(price))
        {
            reason = "Pas assez d'U.V. : il vous en manque " + MoneyFormat.Number(price - wallet.Balance) + ".";
            return false;
        }

        reason = null;
        return true;
    }
}

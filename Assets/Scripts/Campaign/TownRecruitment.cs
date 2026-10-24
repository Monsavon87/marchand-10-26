using System;
using UnityEngine;

/// <summary>
/// Recruiting in town: a new unit joins the caravan's group, at full health, for the price of its offer, as long
/// as the caravan has the U.V. and the group is not full. It says why when a recruitment is not possible.
/// </summary>
public class TownRecruitment : MonoBehaviour
{
    [SerializeField] private PartyRoster party;
    [SerializeField] private Wallet wallet;
    [SerializeField] private PartyCapacity capacity;
    [SerializeField] private RecruitmentSettings settings;

    /// <summary>Raised after a unit joined the group.</summary>
    public event Action<PartyMember> Recruited = delegate { };

    public RecruitmentSettings Settings => settings;
    public PartyRoster Party => party;
    public Wallet Wallet => wallet;
    public int MaxSize => capacity.MaxSize;

    private void Start()
    {
        if (party == null || wallet == null || capacity == null || settings == null)
        {
            Debug.LogError("TownRecruitment: a serialized reference is not assigned.", this);
            enabled = false;
        }
    }

    /// <summary>Can this offer be recruited now? If not, 'reason' says why (shown to the player).</summary>
    public bool CanRecruit(RecruitOffer offer, out string reason)
    {
        if (offer == null || offer.definition == null)
        {
            reason = "offre invalide";
            return false;
        }

        if (party.Members.Count >= capacity.MaxSize)
        {
            reason = "groupe complet (" + party.Members.Count + " / " + capacity.MaxSize + ")";
            return false;
        }

        if (!wallet.CanAfford(offer.price))
        {
            reason = "pas assez d'U.V. (" + MoneyFormat.Number(wallet.Balance) + " / " + MoneyFormat.Number(offer.price) + ")";
            return false;
        }

        reason = null;
        return true;
    }

    /// <summary>Recruits one unit of this offer. False (and nothing changes) when it is not possible.</summary>
    public bool Recruit(RecruitOffer offer)
    {
        if (!CanRecruit(offer, out string reason))
        {
            Debug.Log("[Recrutement] refusé : " + reason, this);
            return false;
        }

        // A free offer (price 0) costs nothing and needs no withdrawal.
        if (offer.price > 0)
        {
            WalletResult payment = wallet.TryWithdraw(offer.price);
            if (payment != WalletResult.Accepted)
            {
                Debug.LogError("TownRecruitment: the payment was refused (" + payment + ") although the price was checked.", this);
                return false;
            }
        }

        var member = new PartyMember { name = NextName(offer.baseName), definition = offer.definition, currentHealth = 0 };
        party.AddMember(member);
        Recruited(member);
        return true;
    }

    // The base name if nobody has it yet, else the base name with the first free number (2, 3...).
    private string NextName(string baseName)
    {
        if (!party.HasMemberNamed(baseName))
        {
            return baseName;
        }

        int number = 2;
        while (party.HasMemberNamed(baseName + " " + number))
        {
            number++;
        }

        return baseName + " " + number;
    }
}

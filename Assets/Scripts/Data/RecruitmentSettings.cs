using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>One kind of unit a town offers: what it is, what it costs, and the name new recruits get.</summary>
[Serializable]
public class RecruitOffer
{
    [Tooltip("Name of the recruits: the first one takes it as is, the next ones get a number (Fantassin 2...).")]
    public string baseName = "Recrue";
    public UnitDefinition definition;
    [Min(0)] public int price = 30;
}

/// <summary>
/// What towns offer to recruit, and the base size limit of the caravan's group. Pure data, editable in the
/// Inspector. The limit is read through PartyCapacity, which will later take the player's stats into account.
/// </summary>
[CreateAssetMenu(fileName = "RecruitmentSettings", menuName = "Marchand/Recruitment Settings")]
public class RecruitmentSettings : ScriptableObject
{
    [SerializeField] private List<RecruitOffer> offers = new List<RecruitOffer>();
    [Tooltip("How many units the caravan's group can hold, before any bonus from the player's stats.")]
    [Min(1)] [SerializeField] private int basePartySize = 4;

    public IReadOnlyList<RecruitOffer> Offers => offers;
    public int BasePartySize => basePartySize;
}

using UnityEngine;

/// <summary>
/// How many units the caravan's group can hold. The one place that answers it: for now the base size of the
/// recruitment settings; later the player's stats will add to it here, and nothing else will need to change.
/// </summary>
public class PartyCapacity : MonoBehaviour
{
    [SerializeField] private RecruitmentSettings settings;

    public int MaxSize => settings != null ? settings.BasePartySize : 1;

    private void Awake()
    {
        if (settings == null)
        {
            Debug.LogError("PartyCapacity: settings is not assigned.", this);
        }
    }
}

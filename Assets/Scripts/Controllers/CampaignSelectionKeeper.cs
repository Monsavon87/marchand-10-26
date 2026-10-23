using UnityEngine;

/// <summary>
/// On the campaign map the player has a single pawn, the caravan: it is always selected. Whenever the selection
/// becomes empty (Escape, a click in the void, the map waking up after a combat), the caravan is selected again.
/// </summary>
public class CampaignSelectionKeeper : MonoBehaviour
{
    [SerializeField] private SelectionManager selection;
    [SerializeField] private SelectableUnit caravan;

    private void Start()
    {
        if (selection == null || caravan == null)
        {
            Debug.LogError("CampaignSelectionKeeper: selection or caravan is not assigned.", this);
            enabled = false;
        }
    }

    // Checked after everything else this frame, so whatever emptied the selection has already run.
    private void LateUpdate()
    {
        if (selection.Selected.Count == 0 && caravan.isActiveAndEnabled)
        {
            selection.SelectOnly(caravan);
        }
    }
}

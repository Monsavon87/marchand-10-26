using UnityEngine;

/// <summary>
/// The painted figurine shown in place of the unit's capsule: base, body and head are on the prefab (team colours),
/// and the equipment of the unit type (UnitDefinition.FigurineGear) is added when the unit is spawned.
/// Visual only: the capsule keeps the collider and the navigation, and nothing here has a collider.
/// </summary>
public class UnitFigurine : MonoBehaviour
{
    [Tooltip("Where the equipment of the unit type goes: a child of the figurine, in the unit's own space.")]
    [SerializeField] private Transform gearSocket;

    private GameObject gear;

    /// <summary>Puts on the equipment of this unit type, replacing any previous one. A type without equipment shows none.</summary>
    public void ShowGear(UnitDefinition definition)
    {
        if (gearSocket == null)
        {
            Debug.LogError("UnitFigurine: gearSocket is not assigned.", this);
            return;
        }

        if (gear != null)
        {
            Destroy(gear);
            gear = null;
        }

        if (definition != null && definition.FigurineGear != null)
        {
            gear = Instantiate(definition.FigurineGear, gearSocket, false);
        }
    }
}

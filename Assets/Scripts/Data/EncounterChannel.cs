using System;
using UnityEngine;

/// <summary>
/// The link between the campaign map and the combat arena, as a shared asset both scenes reference: the map puts
/// the encounter in, the arena reads it, writes the result, and asks to go back. Runtime state only: it is emptied
/// when play stops, so a stale encounter never leaks into the next session.
/// </summary>
[CreateAssetMenu(fileName = "EncounterChannel", menuName = "Marchand/Encounter Channel")]
public class EncounterChannel : ScriptableObject
{
    [NonSerialized] private EncounterData current;

    /// <summary>The encounter being fought, or null.</summary>
    public EncounterData Current => current;

    /// <summary>Raised when the arena is done and the map should take over again.</summary>
    public event Action ReturnRequested = delegate { };

    private void OnEnable()
    {
        Application.quitting += Clear;
    }

    private void OnDisable()
    {
        Application.quitting -= Clear;
    }

    public void Begin(EncounterData encounter)
    {
        current = encounter;
    }

    public void Clear()
    {
        current = null;
    }

    public void RequestReturn()
    {
        ReturnRequested();
    }
}

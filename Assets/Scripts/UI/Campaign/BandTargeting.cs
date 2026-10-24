using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// On the campaign map, the enemy band under the mouse cursor gets a red ring on the ground and a label with what
/// it is made of and what beating it is worth, so the player knows what a click on it (a chase) will lead to.
/// Read-only: it only reads the band's roster.
/// </summary>
public class BandTargeting : MonoBehaviour
{
    [SerializeField] private UnitTeam caravanTeam;
    [Tooltip("Optional: falls back to the main camera.")]
    [SerializeField] private Camera viewCamera;
    [Tooltip("Flat ring shown under the band. It must have no collider, or it would block clicks.")]
    [SerializeField] private Transform ring;
    [SerializeField] private TextMesh label;
    [SerializeField] private float ringHeight = 0.1f;
    [SerializeField] private float labelHeight = 6f;

    private readonly StringBuilder text = new StringBuilder(128);
    private readonly List<string> kinds = new List<string>(4);
    private readonly List<int> counts = new List<int>(4);
    private PartyRoster hovered;

    private void Start()
    {
        if (viewCamera == null)
        {
            viewCamera = Camera.main;
        }

        if (caravanTeam == null || viewCamera == null || ring == null || label == null)
        {
            Debug.LogError("BandTargeting: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.font = font;
        label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        Hide();
    }

    private void Update()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || UiPointerGuard.IsPointerOverUi
            || !Physics.Raycast(viewCamera.ScreenPointToRay(mouse.position.ReadValue()), out RaycastHit hit))
        {
            Hide();
            return;
        }

        UnitTeam team = hit.collider.GetComponentInParent<UnitTeam>();
        PartyRoster band = team != null && team.Team != caravanTeam.Team ? team.GetComponent<PartyRoster>() : null;
        if (band == null)
        {
            Hide();
            return;
        }

        if (band != hovered)
        {
            hovered = band;
            label.text = Describe(band);
        }

        Vector3 position = band.transform.position;
        ring.position = new Vector3(position.x, position.y - 1f + ringHeight, position.z);
        ring.gameObject.SetActive(true);
        label.transform.position = position + Vector3.up * labelHeight;
        label.transform.rotation = viewCamera.transform.rotation;
        label.gameObject.SetActive(true);
    }

    // "Bande ennemie / 2 Fantassins, 1 Archer / Butin : 2 500 U.V.".
    private string Describe(PartyRoster band)
    {
        kinds.Clear();
        counts.Clear();
        for (int i = 0; i < band.Members.Count; i++)
        {
            UnitDefinition definition = band.Members[i].definition;
            string kind = definition != null ? definition.DisplayName : "Inconnu";
            int index = kinds.IndexOf(kind);
            if (index < 0)
            {
                kinds.Add(kind);
                counts.Add(1);
            }
            else
            {
                counts[index]++;
            }
        }

        text.Clear().Append("Bande ennemie\n");
        for (int i = 0; i < kinds.Count; i++)
        {
            if (i > 0)
            {
                text.Append(", ");
            }

            text.Append(counts[i]).Append(' ').Append(kinds[i]);
            if (counts[i] > 1)
            {
                text.Append('s');
            }
        }

        text.Append("\nButin : ").Append(MoneyFormat.WithUnit(band.Loot));
        return text.ToString();
    }

    private void Hide()
    {
        hovered = null;
        ring.gameObject.SetActive(false);
        label.gameObject.SetActive(false);
    }
}

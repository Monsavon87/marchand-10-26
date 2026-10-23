using UnityEngine;

/// <summary>
/// The name of a town floating above it on the campaign map, always turned toward the camera. Read from the town.
/// </summary>
public class TownLabel : MonoBehaviour
{
    [SerializeField] private PointOfInterest town;
    [SerializeField] private TextMesh label;
    [Tooltip("Optional: falls back to the main camera.")]
    [SerializeField] private Camera viewCamera;

    private void Start()
    {
        if (viewCamera == null)
        {
            viewCamera = Camera.main;
        }

        if (town == null || label == null || viewCamera == null)
        {
            Debug.LogError("TownLabel: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.font = font;
        label.GetComponent<MeshRenderer>().sharedMaterial = font.material;
        label.text = town.DisplayName;
    }

    private void LateUpdate()
    {
        transform.rotation = viewCamera.transform.rotation;
    }
}

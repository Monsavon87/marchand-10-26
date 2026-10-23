using UnityEngine;

/// <summary>
/// A health bar floating above a unit: a background strip and a colored strip that shrinks from the right as hit
/// points are lost, always turned toward the camera. Blue for the player's side, red for the enemy's. It only reads
/// the unit's health and side. Hidden when the unit is dead.
/// </summary>
public class UnitHealthBar : MonoBehaviour
{
    [SerializeField] private Health health;
    [SerializeField] private UnitTeam team;
    [Tooltip("The colored strip: a child with its pivot at its center, as wide as the background when full.")]
    [SerializeField] private Transform fill;
    [SerializeField] private Renderer fillRenderer;
    [SerializeField] private Material playerMaterial;
    [SerializeField] private Material enemyMaterial;
    [Tooltip("Optional: falls back to the main camera.")]
    [SerializeField] private Camera viewCamera;
    [Tooltip("Width of the bar in local units, when full.")]
    [Min(0.1f)] [SerializeField] private float width = 1.2f;
    [Tooltip("Height of the bar in local units.")]
    [Min(0.01f)] [SerializeField] private float height = 0.12f;

    private float lastFraction = -1f;

    private void Start()
    {
        if (viewCamera == null)
        {
            viewCamera = Camera.main;
        }

        if (health == null || team == null || fill == null || fillRenderer == null || playerMaterial == null
            || enemyMaterial == null || viewCamera == null)
        {
            Debug.LogError("UnitHealthBar: a serialized reference is not assigned.", this);
            enabled = false;
            return;
        }

        fillRenderer.sharedMaterial = team.Team == Team.Player ? playerMaterial : enemyMaterial;
        UpdateBar(true);
    }

    private void LateUpdate()
    {
        // Facing the camera: the bar keeps the camera's rotation, whatever way the unit turns.
        transform.rotation = viewCamera.transform.rotation;
        UpdateBar(false);
    }

    private void UpdateBar(bool force)
    {
        float fraction = health.Max > 0 ? Mathf.Clamp01((float)health.Current / health.Max) : 0f;
        if (!force && Mathf.Approximately(fraction, lastFraction))
        {
            return;
        }

        lastFraction = fraction;
        // The strip shrinks toward the left edge of the bar.
        fill.localScale = new Vector3(Mathf.Max(0.0001f, width * fraction), height * 0.8f, 1f);
        fill.localPosition = new Vector3(-width * 0.5f + width * fraction * 0.5f, 0f, -0.001f);
        gameObject.SetActive(!health.IsDead);
    }
}

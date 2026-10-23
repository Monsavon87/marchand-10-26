using UnityEngine;

/// <summary>
/// Shows a short text that rises and fades above a unit that was attacked: the damage, or "Raté".
/// Listens to CombatEvents. A small pool of TextMesh objects is made once, so showing a text allocates nothing.
/// </summary>
public class FloatingTextSpawner : MonoBehaviour
{
    private const int PoolSize = 16;

    [Tooltip("Optional: falls back to the main camera. The texts always face it.")]
    [SerializeField] private Camera viewCamera;
    [Tooltip("Height above the unit's position where a text starts, in meters.")]
    [SerializeField] private float startHeight = 2.2f;
    [SerializeField] private float riseSpeed = 1.0f;
    [Tooltip("Seconds a text stays visible.")]
    [SerializeField] private float lifetime = 1.6f;
    [SerializeField] private float characterSize = 0.12f;
    [SerializeField] private int fontSize = 64;
    [SerializeField] private Color damageColor = new Color(1f, 0.35f, 0.3f);
    [SerializeField] private Color missColor = new Color(0.85f, 0.85f, 0.85f);

    private TextMesh[] meshes;
    private float[] ages;
    private bool[] active;
    private Color[] baseColors;
    private int nextIndex;

    private void Awake()
    {
        if (viewCamera == null)
        {
            viewCamera = Camera.main;
        }

        if (viewCamera == null)
        {
            Debug.LogError("FloatingTextSpawner: no camera.", this);
            enabled = false;
            return;
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        meshes = new TextMesh[PoolSize];
        ages = new float[PoolSize];
        active = new bool[PoolSize];
        baseColors = new Color[PoolSize];
        for (int i = 0; i < PoolSize; i++)
        {
            var go = new GameObject("FloatingText_" + i);
            go.transform.SetParent(transform, false);
            TextMesh mesh = go.AddComponent<TextMesh>();
            mesh.font = font;
            mesh.fontSize = fontSize;
            mesh.characterSize = characterSize;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.alignment = TextAlignment.Center;
            go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            go.SetActive(false);
            meshes[i] = mesh;
        }
    }

    private void OnEnable()
    {
        CombatEvents.AttackResolved += OnAttackResolved;
    }

    private void OnDisable()
    {
        CombatEvents.AttackResolved -= OnAttackResolved;
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;
        for (int i = 0; i < PoolSize; i++)
        {
            if (!active[i])
            {
                continue;
            }

            ages[i] += deltaTime;
            if (ages[i] >= lifetime)
            {
                active[i] = false;
                meshes[i].gameObject.SetActive(false);
                continue;
            }

            Transform t = meshes[i].transform;
            t.position += Vector3.up * (riseSpeed * deltaTime);
            t.rotation = viewCamera.transform.rotation;
            Color color = baseColors[i];
            color.a = 1f - ages[i] / lifetime;
            meshes[i].color = color;
        }
    }

    private void OnAttackResolved(AttackReport report)
    {
        Show(report.DefenderPosition, report.Result.Hit ? "-" + report.Result.Damage : "Raté",
            report.Result.Hit ? damageColor : missColor);
    }

    private void Show(Vector3 position, string text, Color color)
    {
        int index = nextIndex;
        nextIndex = (nextIndex + 1) % PoolSize;

        TextMesh mesh = meshes[index];
        mesh.text = text;
        baseColors[index] = color;
        mesh.color = color;
        ages[index] = 0f;
        active[index] = true;
        mesh.transform.position = position + Vector3.up * startHeight;
        mesh.transform.rotation = viewCamera.transform.rotation;
        mesh.gameObject.SetActive(true);
    }
}

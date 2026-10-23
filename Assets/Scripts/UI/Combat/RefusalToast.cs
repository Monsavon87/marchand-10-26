using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A short message at the top of the screen when one of the player's orders is refused or cannot be carried out
/// ("Impossible : la ligne de tir est bloquée"), or to say what to do next. It listens to
/// CombatEvents.OrderRefused and ignores the enemy's refusals. It fades out after a few seconds of real time.
/// </summary>
public class RefusalToast : MonoBehaviour
{
    [SerializeField] private Text label;
    [Tooltip("Seconds the message stays fully visible.")]
    [Min(0f)] [SerializeField] private float visibleTime = 3f;
    [Tooltip("Seconds the message takes to fade out.")]
    [Min(0.01f)] [SerializeField] private float fadeTime = 0.6f;

    private float shownAt = float.NegativeInfinity;
    private Color baseColor;

    private void Start()
    {
        if (label == null)
        {
            Debug.LogError("RefusalToast: label is not assigned.", this);
            enabled = false;
            return;
        }

        baseColor = label.color;
        label.text = string.Empty;
    }

    private void OnEnable()
    {
        CombatEvents.OrderRefused += OnOrderRefused;
    }

    private void OnDisable()
    {
        CombatEvents.OrderRefused -= OnOrderRefused;
    }

    private void OnOrderRefused(Team side, string unitName, string reason)
    {
        if (side != Team.Player || label == null)
        {
            return;
        }

        label.text = string.IsNullOrEmpty(unitName) ? char.ToUpper(reason[0]) + reason.Substring(1) : unitName + " : " + reason;
        shownAt = Time.unscaledTime;
    }

    private void Update()
    {
        float age = Time.unscaledTime - shownAt;
        float alpha = age <= visibleTime ? 1f : Mathf.Clamp01(1f - (age - visibleTime) / fadeTime);
        Color color = baseColor;
        color.a = baseColor.a * alpha;
        label.color = color;
    }
}

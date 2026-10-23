using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Two arrow buttons that scroll a scroll view up or down by a fixed number of pixels per click, for players who
/// prefer clicking to the mouse wheel or the scroll bar.
/// </summary>
public class ScrollStepButtons : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Button upButton;
    [SerializeField] private Button downButton;
    [Tooltip("How far one click scrolls, in pixels of content (about three lines of the journal).")]
    [Min(1f)] [SerializeField] private float step = 54f;

    private void Start()
    {
        if (scrollRect == null || upButton == null || downButton == null)
        {
            Debug.LogError("ScrollStepButtons: a serialized reference is not assigned.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        if (upButton != null && downButton != null)
        {
            upButton.onClick.AddListener(ScrollUp);
            downButton.onClick.AddListener(ScrollDown);
        }
    }

    private void OnDisable()
    {
        if (upButton != null && downButton != null)
        {
            upButton.onClick.RemoveListener(ScrollUp);
            downButton.onClick.RemoveListener(ScrollDown);
        }
    }

    private void ScrollUp()
    {
        Scroll(step);
    }

    private void ScrollDown()
    {
        Scroll(-step);
    }

    // Moves the view by 'pixels' (positive = toward the top), converted to the scroll view's 0..1 position.
    private void Scroll(float pixels)
    {
        float hidden = scrollRect.content.rect.height - scrollRect.viewport.rect.height;
        if (hidden <= 0f)
        {
            return;
        }

        scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition + pixels / hidden);
    }
}

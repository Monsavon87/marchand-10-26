using UnityEngine;

/// <summary>
/// The layering rule of the interface: a window that opens comes to the front, over the screen's permanent display
/// (clock, group, money and cargo) and over any window that was already open. Every window calls this when it opens,
/// so the result never depends on the order the objects happen to have in the scene.
/// </summary>
public static class UiLayering
{
    /// <summary>
    /// Brings the window to the front of its canvas: whatever is given (the window, or anything inside it) is
    /// raised through the child of the canvas that holds it.
    /// </summary>
    public static void BringToFront(Transform window)
    {
        Canvas canvas = window.GetComponentInParent<Canvas>(true);
        if (canvas == null)
        {
            Debug.LogError("UiLayering: '" + window.name + "' is not inside a canvas, it cannot be brought to the front.", window);
            return;
        }

        Transform canvasTransform = canvas.rootCanvas.transform;
        Transform top = window;
        while (top.parent != null && top.parent != canvasTransform)
        {
            top = top.parent;
        }

        top.SetAsLastSibling();
    }
}

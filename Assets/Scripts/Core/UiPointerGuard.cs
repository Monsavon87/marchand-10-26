using UnityEngine.EventSystems;

/// <summary>
/// Tells the input scripts whether the mouse pointer is over an interface element (a button, a panel), so that a
/// click on the interface is not also taken as a click in the world.
/// </summary>
public static class UiPointerGuard
{
    public static bool IsPointerOverUi => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
}

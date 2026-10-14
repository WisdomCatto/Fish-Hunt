using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Ditambahkan otomatis oleh MainMenuController ke tiap label menu.
/// Meneruskan hover dan klik mouse ke controller.
/// </summary>
public class MenuItemHover : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [HideInInspector] public MainMenuController controller;
    [HideInInspector] public int index;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (controller != null) controller.Select(index);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (controller == null) return;
        controller.Select(index);
        controller.Activate();
    }
}

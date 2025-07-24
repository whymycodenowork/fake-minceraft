using UnityEngine;
using Items;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>  
/// A slot in an <see cref="Inventory"/>  
/// </summary>  
public class InventorySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    /// <summary>  
    /// Whether the slot is currently being left clicked.  
    /// </summary>  
    public bool isLeftClicked;
    /// <summary>  
    /// Whether the slot is currently being right clicked.  
    /// </summary>  
    public bool isRightClicked;
    /// <summary>  
    /// Whether the mouse is currently over this slot.  
    /// </summary>  
    public bool mouseOver;
    public RawImage image; // The UI image component to display the item texture  
    public TMPro.TextMeshProUGUI text;
    public Image backgroundImage;

    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left) // Left click  
        {
            isLeftClicked = true;
        }
        else if (eventData.button == PointerEventData.InputButton.Right) // Right click  
        {
            isRightClicked = true;
        }
    }

    void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
    {
        mouseOver = true;
    }

    void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
    {
        mouseOver = false;
    }
}

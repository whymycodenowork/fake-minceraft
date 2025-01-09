using UnityEngine;
using UnityEngine.EventSystems;

public class InventorySlot : MonoBehaviour, IPointerDownHandler
{
    public byte slotNumber;
    public InventoryManager inventoryManager;
    public void OnPointerDown(PointerEventData pointerEventData)
    {
        inventoryManager.ClickInventorySlot(slotNumber);
    }
}

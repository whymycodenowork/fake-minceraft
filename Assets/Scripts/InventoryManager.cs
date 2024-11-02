using System;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public Image[] hotbarSlots; // Array of UI Images representing each hotbar slot
    public float scrollSpeed;
    private float hotbarSlot = 1;

    private Color defaultColor = Color.white;      // Default color for unselected slots
    private Color selectedColor = Color.gray;      // Color for the selected slot

    public GameObject crosshair;
    public GameObject inventory;

    void Update()
    {
        if (!inventory.activeSelf)
        {
            crosshair.SetActive(true);
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            if (scroll > 0f)
            {
                hotbarSlot = (hotbarSlot + scrollSpeed) % hotbarSlots.Length;
                UpdateHotbarUI();
            }
            else if (scroll < 0f)
            {
                hotbarSlot = (hotbarSlot - scrollSpeed + hotbarSlots.Length) % hotbarSlots.Length;
                UpdateHotbarUI();
            }
        }
        else
        {
            crosshair.SetActive(false);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            inventory.SetActive(!inventory.activeSelf);
        }
    }

    void UpdateHotbarUI()
    {
        // Loop through each slot and apply the selected/unselected color
        for (int i = 0; i < hotbarSlots.Length; i++)
        {
            hotbarSlots[i].color = (i == Math.Round(hotbarSlot)) ? selectedColor : defaultColor;
        }
    }

    public void OpenInventory(byte inventoryType)
    {
        switch (inventoryType)
        {
            default:
                Debug.LogWarning("invalid inventory type");
                break;
            case 0:
                inventory.SetActive(true);
                break;
            case 1:
                break;
            case 2:
                break;
        }
    }
}

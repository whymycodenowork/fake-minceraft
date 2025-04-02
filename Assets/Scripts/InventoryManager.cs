using System;
using System.Collections.Generic;
using Items;
using Items.BlockItems;
using Items.MiscItems;
using Items.ToolItems;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public PauseManager pauseManager;
    public GameObject[] hotbarSlots; // Array of GameObjects representing each hotbar slot
    public Item[] HotbarItems;
    public GameObject inventorySlots;
    public List<GameObject> inventorySlotsList = new();
    public List<Item> InventoryItems = new();
    public float scrollSpeed;
    public bool inventoryOpen;
    public float hotbarSlot = 1; // The selected hotbar slot
    public Item SelectedItem; // The Item that you have clicked on and is now following your cursor

    private readonly Color _defaultColor = new(255, 255, 255, 100);      // Default color for unselected slots
    private readonly Color _selectedColor = new(200, 200, 200, 100);      // Color for the selected slot

    public GameObject crosshair;
    public GameObject inventory;

    private void Awake()
    {
        var inventorySlotsTransform = inventorySlots.GetComponentsInChildren<Transform>(true);
        for (var i = 0; i < inventorySlotsTransform.Length; i++)
        {
            if (inventorySlotsTransform[i] == inventorySlots.transform) continue;
            inventorySlotsList.Add(inventorySlotsTransform[i].gameObject);
        }
        for (var j = 0; j < inventorySlotsList.Count; j++)
        {
            InventoryItems.Add(new Nothing());
        }
        HotbarItems = new Item[9]
        {
            new WoodLog(),
            new WoodPickaxe(),
            new WaterBucket(),
            new Cobblestone(),
            new Stone(),
            new Dirt(),
            new Grass(),
            new WoodPlanks(),
            new Nothing(),
        };
    }
    private void Start()
    {
        UpdateHotbarUI();
    }
    void Update()
    {
        if (!inventory.activeSelf)
        {
            inventoryOpen = false;
            crosshair.SetActive(true);
            var scroll = Input.GetAxis("Mouse ScrollWheel");

            if (scroll > 0f)
            {
                hotbarSlot = (hotbarSlot + scrollSpeed) % 8;
                UpdateHotbarUI();
            }
            else if (scroll < 0f)
            {
                hotbarSlot = (hotbarSlot - scrollSpeed + hotbarSlots.Length) % 8;
                UpdateHotbarUI();
            }
        }
        else
        {
            inventoryOpen = true;
            crosshair.SetActive(false);
        }

        if (Input.GetKeyDown(KeyCode.E) && !pauseManager.isPaused)
        {
            inventory.SetActive(!inventory.activeSelf);
            UpdateInventoryUI();
        }
    }

    void UpdateHotbarUI()
    {
        // Loop through each slot and apply the color and image
        for (var i = 0; i < hotbarSlots.Length; i++)
        {
            var currentHotbarSlot = hotbarSlots[i];
            currentHotbarSlot.GetComponent<Image>().color = (i == Math.Round(hotbarSlot)) ? _selectedColor : _defaultColor;
            var currentHotbarImage = currentHotbarSlot.transform.GetChild(0).GetComponent<RawImage>();
            if (HotbarItems[i] is BlockItem blockItem)
            {
                currentHotbarImage.texture = TextureManager.blockItemTextures[blockItem.BlockToPlace.ID, 0];
            }
            else
            {
                currentHotbarImage.texture = TextureManager.itemTextures[HotbarItems[i].TextureID];
            }
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

    public void UpdateInventoryUI()
    {
        
    }

    public void ClickInventorySlot(byte slotNumber)
    {
        var item = InventoryItems[slotNumber];

        if (SelectedItem.GetType() == item.GetType())
        {
            SelectedItem = item.Add(SelectedItem);
        }
        else // Swap if not the same type
        {
            var temp = SelectedItem;
            SelectedItem = item;
            InventoryItems[slotNumber] = temp;
        }
        UpdateInventoryUI(); // At the end, update the inventory
    }
}

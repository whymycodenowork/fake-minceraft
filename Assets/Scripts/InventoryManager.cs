using Items;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages <see cref="Inventory">inventories</see>.
/// </summary>
/// <remarks>
/// Manages all inventories, not just the player's.
/// Not to be confused with <see cref="Inventory"/>, which is what this class manages.
/// </remarks>
public class InventoryManager : MonoBehaviour
{
    // Inventory items and slots are stored in the Player or other containers
    public Item selectedItem; // The currently selected item in the inventory
    [SerializeField] private RawImage selectedItemImage; // The UI image to display the selected item texture
    [SerializeField] private TMPro.TextMeshProUGUI selectedItemCountText; // The UI text to display the selected item's count
    [SerializeField] private GameObject panel; // The panel that shows the details of the item that is hovered over
    [SerializeField] private TMPro.TextMeshProUGUI nameText; // The UI text to display the name of the item that is hovered over
    [SerializeField] private TMPro.TextMeshProUGUI descriptionText; // The UI text to display the description of the item that is hovered over
    [SerializeField] private RectTransform canvas; // The canvas that holds all the UI
    /// <summary>
    /// The current items each inventory is displaying.
    /// </summary>
    public List<Item>[] inventoryItems; // not a dictionary because it's slightly faster, so cast inventoryType to int when accessing
    /// <summary>
    /// The inventories that are currently open.
    /// </summary>
    public bool[] openInventories =
    {
        true,  // Hotbar is always active
        false, // Player inventory
        false // Container inventory
    };
    public int hotbarIndex = 0;
    public Item HeldItem
    {
        get => inventoryItems[0][hotbarIndex]; // Get the item in the currently selected hotbar slot

        set => inventoryItems[0][hotbarIndex] = value;
    }

    public GameObject inventorySlotPrefab; // Prefab for inventory slots, assign in inspector

    // Singleton instance
    public static InventoryManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject); // Ensure only one instance exists
        }

        selectedItem = Empty.Instance; // Initialize selected item to None
    }

    private void Start()
    {
        inventoryItems = new List<Item>[]
        {
            new(), // Hotbar items
            Player.Instance.items, // Player inventory items
            null
        };
        for (int i = 0; i < 9; i++)
        {
            inventoryItems[0].Add(Empty.Instance); // Initialize hotbar with empty items
        }
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (openInventories[1])
            {
                openInventories[1] = false; // Close the player's inventory
                openInventories[2] = false; // Close other inventories
                Player.Instance.GiveItem(selectedItem); // Give the selected item back to the player
                selectedItem = Empty.Instance; // Reset selected item to None
            }
            else
            {
                openInventories[1] = true; // Open the player's inventory
            }
            inventories[1].gameObject.SetActive(openInventories[1]); // Show or hide the inventory UI
        }
        for (int i = 0; i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i))
            {
                hotbarIndex = i;
            }
        }
        for (int i = 0; i < inventories[0].slotCount; i++)
        {
            Transform child = inventories[0].InventorySlots.GetChild(i);
            child.GetComponent<Image>().color = i == hotbarIndex ? new Color(1, 1, 1, 0.5f) : new Color(0.5f, 0.5f, 0.5f, 0.5f); // Highlight the selected hotbar slot
        }
        HandleInventory(inventories[0]);

        if (!(openInventories[1] || openInventories[2])) // Only handle clicks if no inventory is open
        {
            if (Input.GetKeyDown(KeyCode.Mouse0)) // Left click
            {
                HeldItem.UseLeft();
                if (HeldItem.count == 0)
                {
                    HeldItem = Empty.Instance; // Reset held item if count is 0
                }
            }
            if (Input.GetKeyDown(KeyCode.Mouse1)) // Right click
            {
                HeldItem.UseRight();
                if (HeldItem.count == 0)
                {
                    HeldItem = Empty.Instance; // Reset held item if count is 0
                }
            }
            return;
        }
        for (int i = 1; i < openInventories.Length; i++)
        {
            if (openInventories[i])
            {
                HandleInventory(inventories[i]);
            }
        }
    }

    private void LateUpdate()
    {
        selectedItemImage.texture = selectedItem is IBlockTexture ?
            TextureManager.BlockItemTextures[selectedItem.TextureID] :
            TextureManager.ItemTextures[selectedItem.TextureID]; // Update the held item texture
        selectedItemCountText.text = selectedItem.count > 1 ? selectedItem.count.ToString() : ""; // Update the text to show item count
        transform.localPosition = Input.mousePosition - canvas.localPosition;
    }

    private void HandleInventory(Inventory inventory)
    {
        List<Item> inventoryItemsList = inventoryItems[(int)inventory.type];
        int countSlots = inventory.InventorySlots.childCount;
        int countItems = inventoryItemsList.Count;

        bool shiftHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool altHeld = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        bool hovered = false;

        // Make new slots if needed
        for (int i = countSlots; i < countItems; i++)
        {
            // Instantiate a new slot as a child of inventory.InventorySlots
            InventorySlot newSlot = InstantiateSlot(inventory.InventorySlots);
            newSlot.gameObject.SetActive(true);
        }

        // Deactivate excess slots
        for (int i = countItems; i < countSlots; i++)
        {
            inventory.InventorySlots.GetChild(i).gameObject.SetActive(false);
        }

        // Loop through the items and update the slots
        for (int i = 0; i < countItems; i++)
        {
            InventorySlot slot = inventory.InventorySlots.GetChild(i).GetComponent<InventorySlot>();
            Item item = inventoryItemsList[i];

            // Left click handling
            if (slot.isLeftClicked)
            {
                if (altHeld && item != Empty.Instance)
                {
                    // If Alt is held, toggle the favorited state of the item
                    item.Favorited = !item.Favorited;
                }
                else if (item != Empty.Instance && selectedItem != Empty.Instance)
                {
                    // If both items are the same type, attempt to move as many to one side as possible
                    if (item.GetType() == selectedItem.GetType())
                    {
                        if (shiftHeld)
                        {
                            int spaceLeft = selectedItem.MaxCount - selectedItem.count;
                            int moveCount = Mathf.Min(spaceLeft, item.count);
                            item.count -= moveCount;
                            selectedItem.count += moveCount;
                            if (item.count == 0)
                            {
                                inventoryItemsList[i] = Empty.Instance; // Set to empty if count is 0
                            }
                        }
                        else
                        {
                            int spaceLeft = item.MaxCount - item.count;
                            int moveCount = Mathf.Min(spaceLeft, selectedItem.count);
                            selectedItem.count -= moveCount;
                            item.count += moveCount;
                            if (selectedItem.count == 0)
                            {
                                selectedItem = Empty.Instance; // Set to empty if count is 0
                            }
                        }
                    }
                }
                else
                {
                    (selectedItem, inventoryItemsList[i]) = (inventoryItemsList[i], selectedItem); // Swap the items
                }
                slot.isLeftClicked = false;
            }

            // Right click handling
            else if (slot.isRightClicked)
            {
                // If both items are empty, do nothing
                if (item == Empty.Instance && selectedItem == Empty.Instance)
                {
                    // tbh more languanges need a pass keyword
                }
                else if (item != Empty.Instance && selectedItem != Empty.Instance && item.GetType() != selectedItem.GetType())
                {
                    (selectedItem, inventoryItemsList[i]) = (inventoryItemsList[i], selectedItem); // Swap the items if they are different types
                }
                else
                {
                    if (shiftHeld)
                    {
                        (selectedItem, inventoryItemsList[i]) = (inventoryItemsList[i], selectedItem); // Temporarily swap the items
                    }
                    
                    int spaceLeft = item.MaxCount - item.count;

                    int moveCount;
                    if (altHeld)
                    {
                        moveCount = Mathf.Min((selectedItem.count + 1) / 2, spaceLeft);
                    }
                    else
                    {
                        moveCount = Mathf.Min(1, spaceLeft);
                    }

                    moveCount = Mathf.Min(moveCount, selectedItem.count); // Ensure we don't move more than we have

                    selectedItem.count -= moveCount;

                    if (item == Empty.Instance)
                    {
                        inventoryItemsList[i] = System.Activator.CreateInstance(selectedItem.GetType()) as Item; // Create a new instance of the item type
                        item = inventoryItemsList[i]; // Update the item reference
                        item.count = 0; // Initialize the new item count to 0
                    }

                    item.count += moveCount;

                    if (selectedItem.count == 0)
                    {
                        selectedItem = Empty.Instance; // Set to empty if count is 0
                    }

                    if (shiftHeld)
                    {
                        (selectedItem, inventoryItemsList[i]) = (inventoryItemsList[i], selectedItem); // Swap the items back
                    }
                }
                slot.isRightClicked = false;
            }

            // Update slot display with item texture and count
            slot.image.texture = item is IBlockTexture ? TextureManager.BlockItemTextures[item.TextureID] : TextureManager.ItemTextures[item.TextureID];

            // Update the count text
            slot.text.text = item.count > 1 ? item.count.ToString() : "";

            // Display name and description if mouse is hovering over the slot
            if (slot.mouseOver)
            {
                nameText.text = item.Name;
                descriptionText.text = item.Description;
                if (item.Name != "") panel.SetActive(true);
                slot.backgroundImage.color = item.Favorited ? Color.yellow : Color.white; // Highlight favorited items
                hovered = true;
            }
            else
            {
                slot.backgroundImage.color = item.Favorited ? new Color(0.831f, 0.686f, 0.216f) : Color.gray; // Dim non-hovered items
            }
        }
        if (!hovered)
        {
            nameText.text = "";
            descriptionText.text = "";
            panel.SetActive(false);
        }
    }

    private InventorySlot InstantiateSlot(Transform parent)
    {
        GameObject slotObj = Instantiate(inventorySlotPrefab, parent);
        return slotObj.GetComponent<InventorySlot>();
    }

    [SerializeField] private Inventory[] inventories; // Assign in inspector

    public enum InventoryType
    {
        Hotbar,
        Player,
        Container
    }

    public static bool AddItem(List<Item> items, Item item)
    {
        // foreach (Item item2 in items)
        // {
        //     // TODO: add logic
        // }
        // return false;
        throw new System.NotImplementedException("AddItem method is not implemented yet.");
    }

    /// <summary>
    /// Trash the currently selected item.
    /// </summary>
    public void Trash()
    {
        if (selectedItem.Favorited)
        {
            return;
        }

        selectedItem = Empty.Instance; // Reset selected item to None
    }
}

using System.Collections.Generic;
using UnityEngine;
using Items;
using UnityEngine.UI;

/// <summary>
/// Used to display the items in the inventory. Does not handle the logic. That would be done by the <see cref="InventoryManager"/>.
/// </summary>
public class Inventory : MonoBehaviour
{
    /// <summary>
    /// The parent Transform of all the inventory slots in the inventory
    /// </summary>
    public Transform InventorySlots;
    /// <summary>
    /// The number of slots the inventory has.
    /// </summary>
    public int slotCount = 10;

    public InventoryManager.InventoryType type; // Assign in inspector

    private void Start()
    {
        // Instantiate slots
        for (int i = 0; i < slotCount; i++)
        {
            Instantiate(InventoryManager.Instance.inventorySlotPrefab, InventorySlots);
        }
    }
}

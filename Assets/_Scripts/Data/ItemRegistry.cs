using System.Collections.Generic;
using UnityEngine;

public static class ItemRegistry
{
    // Zugriff über String-ID (für Inventar/Commands)
    private static Dictionary<string, ItemDefinition> itemsByID = new Dictionary<string, ItemDefinition>();
    
    // Zugriff über BlockType (für Drops: Block -> Item)
    private static Dictionary<BlockType, ItemDefinition> itemsByBlockType = new Dictionary<BlockType, ItemDefinition>();

    private static bool isInitialized = false;

    public static void Initialize()
    {
        if (isInitialized) return;

        itemsByID.Clear();
        itemsByBlockType.Clear();

        // Lädt ALLE Items aus dem Resources-Ordner
        ItemDefinition[] allItems = Resources.LoadAll<ItemDefinition>("Items");

        foreach (var item in allItems)
        {
            // 1. String Lookup füllen
            if (!string.IsNullOrEmpty(item.itemID))
            {
                if (!itemsByID.ContainsKey(item.itemID))
                    itemsByID.Add(item.itemID, item);
            }

            // 2. Block Lookup füllen (Reverse Lookup für Drops)
            if (item.isPlaceable && item.blockType != BlockType.Air)
            {
                if (!itemsByBlockType.ContainsKey(item.blockType))
                {
                    itemsByBlockType.Add(item.blockType, item);
                }
            }
        }

        isInitialized = true;
        Debug.Log($"ItemRegistry initialized: {itemsByID.Count} Items loaded.");
    }

    // String -> Item
    public static ItemDefinition GetItem(string id)
    {
        if (!isInitialized) Initialize();
        itemsByID.TryGetValue(id, out ItemDefinition item);
        return item;
    }

    // BlockType -> Item (Das ist die Magie für Drops!)
    public static ItemDefinition GetDropItem(BlockType blockType)
    {
        if (!isInitialized) Initialize();
        itemsByBlockType.TryGetValue(blockType, out ItemDefinition item);
        return item; // Gibt null zurück, wenn kein Item für diesen Block existiert
    }
}
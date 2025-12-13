using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "TimoCraft/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    [Header("General")]
    public string itemID;      // Eindeutige ID (z.B. "stone")
    public string displayName; // Anzeigename (z.B. "Stone Block")
    public Sprite icon;
    public int maxStack = 64;

    [Header("Block Data")]
    public bool isPlaceable;    // Kann man das hinstellen?
    public BlockType blockType; // Wenn ja, welche Nummer im Chunk wird es?
}
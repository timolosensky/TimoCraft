using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "TimoCraft/Item Definition")]
public class ItemDefinition : ScriptableObject
{
    public string itemName;
    public Sprite icon;       // Für das UI später
    public bool isBlock;
    public BlockType blockType; // Welcher Block wird gesetzt? (Nur relevant wenn isBlock = true)
    public int maxStack = 64;
}
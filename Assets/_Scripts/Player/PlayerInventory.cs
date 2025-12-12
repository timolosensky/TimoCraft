using UnityEngine;
using Mirror;

public class PlayerInventory : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private int hotbarSize = 9;
    
    // Wir synchronisieren das Inventar nicht jeden Frame,
    // aber für komplexe Spiele nutzt man oft SyncLists. 
    // Fürs Erste halten wir es lokal für den Client + Server Authority checks später.
    
    public ItemStack[] hotbarSlots;
    private int selectedSlotIndex = 0;

    // Event, damit das UI weiß, wann es sich updaten muss
    public event System.Action OnInventoryChanged;
    public event System.Action<int> OnSelectionChanged;

    private void Awake()
    {
        hotbarSlots = new ItemStack[hotbarSize];
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= hotbarSize) return;
        selectedSlotIndex = index;
        OnSelectionChanged?.Invoke(selectedSlotIndex);
    }

    public ItemStack GetSelectedItem()
    {
        return hotbarSlots[selectedSlotIndex];
    }
    
    // Einfache Methode zum Hinzufügen von Items (für Debugging / Creative Mode)
    public void AddItem(ItemDefinition item, int amount)
    {
        // Suche existierenden Stack
        for (int i = 0; i < hotbarSize; i++)
        {
            if (hotbarSlots[i] != null && hotbarSlots[i].item == item)
            {
                hotbarSlots[i].amount += amount;
                OnInventoryChanged?.Invoke();
                return;
            }
        }
        
        // Sonst erster freier Slot
        for (int i = 0; i < hotbarSize; i++)
        {
            if (hotbarSlots[i] == null || hotbarSlots[i].item == null)
            {
                hotbarSlots[i] = new ItemStack(item, amount);
                OnInventoryChanged?.Invoke();
                return;
            }
        }
    }
}
using UnityEngine;
using Mirror;

public class PlayerInventory : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private int hotbarSize = 9;
    
    public ItemStack[] hotbarSlots;
    private int selectedSlotIndex = 0;

    // Events für UI Updates
    public event System.Action OnInventoryChanged;
    public event System.Action<int> OnSelectionChanged;

    [Header("Debug / Creative Mode")]
    [SerializeField] private ItemDefinition[] starterItems; 

private void Start()
    {
        if (isLocalPlayer)
        {
            // UI Verbindung
            HotbarUI ui = FindObjectOfType<HotbarUI>();
            if (ui != null) ui.Initialize(this);
            
            // --- HIER IST DER FIX ---
            // Wir ignorieren alles, was vllt. im Inspector in "hotbarSlots" stand
            // und füllen strikt nach StarterItems.
            if (starterItems != null)
            {
                foreach (var item in starterItems)
                {
                    if (item != null)
                    {
                        AddItemToFirstFreeSlot(item, 64);
                    }
                }
            }
            
            SelectSlot(0);
        }
    }


    private void Awake()
    {
        // Initialisiere das Array komplett leer
        hotbarSlots = new ItemStack[hotbarSize];
        for (int i = 0; i < hotbarSize; i++)
        {
            hotbarSlots[i] = new ItemStack(); // Leere Slots (kein null)
        }
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        // Tasten 1-9 für Slot Auswahl
        for (int i = 0; i < hotbarSize; i++)
        {
            // KeyCode.Alpha1 ist 49. Wir prüfen 1 bis 9.
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                SelectSlot(i);
            }
        }
        
        // Mausrad Scrollen (Optionales Feature)
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f) ChangeSlot(-1);
        if (scroll < 0f) ChangeSlot(1);
    }

    private void ChangeSlot(int direction)
    {
        selectedSlotIndex -= direction;
        if (selectedSlotIndex < 0) selectedSlotIndex = hotbarSize - 1;
        if (selectedSlotIndex >= hotbarSize) selectedSlotIndex = 0;
        SelectSlot(selectedSlotIndex);
    }

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= hotbarSize) return;
        selectedSlotIndex = index;
        OnSelectionChanged?.Invoke(selectedSlotIndex);
    }

    public ItemStack GetSelectedItem()
    {
        if (hotbarSlots == null || selectedSlotIndex >= hotbarSlots.Length) return null;
        return hotbarSlots[selectedSlotIndex];
    }

    // Hilfsfunktion zum Testen: Füllt die Leiste mit Items
    public void AddItemToFirstFreeSlot(ItemDefinition item, int amount)
    {
        // 1. Suche nach existierendem Stack desselben Typs
        for (int i = 0; i < hotbarSize; i++)
        {
            // Prüfung auf null und item match
            if (hotbarSlots[i].item == item)
            {
                hotbarSlots[i].amount += amount;
                OnInventoryChanged?.Invoke();
                return;
            }
        }
        
        // 2. Wenn nicht gefunden, suche ersten leeren Slot
        for (int i = 0; i < hotbarSize; i++)
        {
            // Ein Slot ist frei, wenn item null ist
            if (hotbarSlots[i].item == null)
            {
                hotbarSlots[i] = new ItemStack(item, amount);
                OnInventoryChanged?.Invoke();
                return;
            }
        }
    }
}

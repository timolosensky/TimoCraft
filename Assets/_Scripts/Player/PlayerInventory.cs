using UnityEngine;
using Mirror;

public class PlayerInventory : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private int hotbarSize = 9;
    
    [Header("Setup")]
    [Tooltip("Items, die der Spieler beim Spawn erhält")]
    [SerializeField] private ItemDefinition[] starterItems; 

    // NonSerialized verhindert, dass Unity gespeicherte Daten aus dem Editor lädt
    // und so unsere "saubere" Initialisierung überschreibt.
    [System.NonSerialized]
    public ItemStack[] hotbarSlots;

    private int selectedSlotIndex = 0;

    public event System.Action OnInventoryChanged;
    public event System.Action<int> OnSelectionChanged;

    private void Awake()
    {
        // Initialisiere leere Slots
        hotbarSlots = new ItemStack[hotbarSize];
        for (int i = 0; i < hotbarSize; i++)
        {
            hotbarSlots[i] = new ItemStack(); 
        }
    }

    private void Start()
    {
        if (isLocalPlayer)
        {
            // UI verbinden
            HotbarUI ui = FindObjectOfType<HotbarUI>();
            if (ui != null) ui.Initialize(this);
            
            // Starter Items hinzufügen
            if (starterItems != null)
            {
                foreach (var item in starterItems)
                {
                    if (item != null)
                    {
                        // Wir fügen immer standardmäßig 64 hinzu für den Creative Mode
                        AddItem(item, 64);
                    }
                }
            }
            
            SelectSlot(0);
        }
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        // Tasten 1-9
        for (int i = 0; i < hotbarSize; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) SelectSlot(i);
        }
        
        // Mausrad
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

    /// <summary>
    /// Fügt ein Item intelligent hinzu (Stapeln + Auffüllen leerer Slots)
    /// </summary>
    public void AddItem(ItemDefinition item, int amount)
    {
        if (item == null || amount <= 0) return;

        // SCHRITT 1: Versuche, existierende Stapel aufzufüllen
        for (int i = 0; i < hotbarSize; i++)
        {
            // Wenn wir nichts mehr zu verteilen haben, abbrechen
            if (amount <= 0) break;

            ItemStack slot = hotbarSlots[i];

            // Passt das Item? Und ist noch Platz im Stack?
            if (slot.item == item && slot.amount < item.maxStack)
            {
                int spaceInSlot = item.maxStack - slot.amount;
                int amountToAdd = Mathf.Min(amount, spaceInSlot);

                slot.amount += amountToAdd;
                amount -= amountToAdd;
            }
        }

        // SCHRITT 2: Wenn immer noch Menge übrig ist, fülle leere Slots
        for (int i = 0; i < hotbarSize; i++)
        {
            if (amount <= 0) break;

            ItemStack slot = hotbarSlots[i];

            // Ist der Slot leer?
            if (slot.item == null)
            {
                // Wie viel passt in einen neuen Stack? (Maximal 64 oder was noch übrig ist)
                int amountToAdd = Mathf.Min(amount, item.maxStack);

                slot.item = item;
                slot.amount = amountToAdd;
                amount -= amountToAdd;
            }
        }

        // Wenn amount > 0 ist, ist das Inventar voll (hier könnte man "Inventory Full" anzeigen)
        
        OnInventoryChanged?.Invoke();
    }
}
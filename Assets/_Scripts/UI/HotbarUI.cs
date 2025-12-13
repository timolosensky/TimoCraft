using UnityEngine;
using UnityEngine.UI; // Wichtig für Layouts

public class HotbarUI : MonoBehaviour
{
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform hotbarParent;
    
    private PlayerInventory playerInventory;
    private UI_ItemSlot[] slots; // Wir speichern jetzt direkt die Skripte

    public void Initialize(PlayerInventory inventory)
    {
        playerInventory = inventory;
        playerInventory.OnInventoryChanged += Redraw;
        playerInventory.OnSelectionChanged += UpdateSelection;
        
        CreateSlots();
        Redraw(); // Einmal initial zeichnen
    }

    void CreateSlots()
    {
        // 1. Aufräumen: Alte Slots löschen
        foreach(Transform child in hotbarParent) Destroy(child.gameObject);

        int size = 9; // Fixe Größe für Hotbar
        slots = new UI_ItemSlot[size];

        for (int i = 0; i < size; i++)
        {
            GameObject newSlotObj = Instantiate(slotPrefab, hotbarParent);
            newSlotObj.name = $"Slot_{i}";
            
            // 2. Das Script holen, das wir gerade im Prefab verlinkt haben
            UI_ItemSlot slotScript = newSlotObj.GetComponent<UI_ItemSlot>();
            
            if (slotScript == null)
            {
                Debug.LogError("UI_ItemSlot Prefab hat kein 'UI_ItemSlot' Script!");
                return;
            }

            slots[i] = slotScript;
        }
    }

    void Redraw()
    {
        if (playerInventory == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            // Daten aus dem Inventar holen
            ItemStack stack = null;
            if (i < playerInventory.hotbarSlots.Length)
            {
                stack = playerInventory.hotbarSlots[i];
            }

            // An den Slot übergeben
            slots[i].UpdateSlot(stack);
        }
    }

    void UpdateSelection(int index)
    {
        // Hier kommt später der Auswahl-Rahmen hin
        // z.B. slots[index].selectionOutline.enabled = true;
    }
}
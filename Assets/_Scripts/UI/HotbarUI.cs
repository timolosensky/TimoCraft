using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HotbarUI : MonoBehaviour
{
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Transform hotbarParent;
    
    // Referenz zum lokalen Spieler Inventar
    private PlayerInventory playerInventory;
    
    // Liste der instanziierten UI Slots
    private Image[] slotIcons;
    private TextMeshProUGUI[] slotAmounts;
    private GameObject[] selectionHighlights; // Optional: Rahmen um aktiven Slot

    private void Start()
    {
        // Wir suchen den Spieler erst, wenn er gespawnt ist.
        // Besser: Der Spieler meldet sich beim UI an.
        // Quick & Dirty für jetzt: In Update suchen oder via NetworkClient event.
    }

    public void Initialize(PlayerInventory inventory)
    {
        playerInventory = inventory;
        playerInventory.OnInventoryChanged += Redraw;
        playerInventory.OnSelectionChanged += UpdateSelection;
        
        CreateSlots();
        Redraw();
        UpdateSelection(0);
    }

    void CreateSlots()
    {
        // Bestehende löschen
        foreach(Transform child in hotbarParent) Destroy(child.gameObject);

        int size = 9; // Hardcoded oder aus Inventory lesen
        slotIcons = new Image[size];
        slotAmounts = new TextMeshProUGUI[size];
        selectionHighlights = new GameObject[size];

        for (int i = 0; i < size; i++)
        {
            GameObject newSlot = Instantiate(slotPrefab, hotbarParent);
            // Wir nehmen an, das Prefab hat Image (Hintergrund) -> Image (Icon) -> Text (Menge)
            // Passe diese Zeilen an deine Prefab-Struktur an!
            slotIcons[i] = newSlot.transform.GetChild(0).GetComponent<Image>(); 
            slotAmounts[i] = newSlot.GetComponentInChildren<TextMeshProUGUI>();
            
            // Icon standardmäßig unsichtbar
            slotIcons[i].enabled = false;
            slotAmounts[i].text = "";
        }
    }

    void Redraw()
    {
        if (playerInventory == null) return;

        for (int i = 0; i < playerInventory.hotbarSlots.Length; i++)
        {
            ItemStack stack = playerInventory.hotbarSlots[i];
            if (stack != null && stack.item != null)
            {
                slotIcons[i].sprite = stack.item.icon;
                slotIcons[i].enabled = true;
                slotAmounts[i].text = stack.amount.ToString();
            }
            else
            {
                slotIcons[i].enabled = false;
                slotAmounts[i].text = "";
            }
        }
    }

    void UpdateSelection(int index)
    {
        // Hier könntest du einen Rahmen (Image) verschieben oder einfärben
        // Zum Beispiel: Alle weiß, ausgewählter Slot grün.
        // Das implementieren wir, wenn die Basics laufen.
    }
}
using UnityEngine;
using Mirror;

public class DroppedItem : NetworkBehaviour
{
    // SyncVar Hook: Wenn sich die ID ändert (z.B. initial beim Spawnen), 
    // wird UpdateVisuals aufgerufen.
    [SyncVar(hook = nameof(OnItemChanged))]
    public string itemID;

    [SyncVar] public int amount = 1;

    [Header("References")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private void Start()
    {
        // --- FIX FÜR EINSAMMELN ---
        // Wir stellen sicher, dass der Collider ein Trigger ist.
        // Ein Trigger lässt Objekte durch (keine physikalische Wand) 
        // und feuert OnTriggerEnter.
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true; 
        }

        // Falls wir Host sind (Server+Client), wird der Hook manchmal nicht automatisch 
        // beim Start gefeuert, daher einmal manuell prüfen:
        if (isClient && !string.IsNullOrEmpty(itemID))
        {
            UpdateVisuals(itemID);
        }
    }

    private void Update()
    {
        if (isClient)
        {
            // Schwebe-Animation
            transform.Rotate(0, 50 * Time.deltaTime, 0);
            float yOffset = Mathf.Sin(Time.time * 2f) * 0.1f;
            
            if(spriteRenderer != null)
                spriteRenderer.transform.localPosition = new Vector3(0, 0.25f + yOffset, 0);
        }
    }

    // Der Hook wird automatisch aufgerufen, wenn der Server die itemID setzt
    void OnItemChanged(string oldID, string newID)
    {
        UpdateVisuals(newID);
    }

    void UpdateVisuals(string id)
    {
        if (spriteRenderer == null) return;
        
        // Statt World.Instance.GetItemByName nutzen wir:
        ItemDefinition def = ItemRegistry.GetItem(id);
        
        if (def != null)
        {
            spriteRenderer.sprite = def.icon;
            spriteRenderer.color = Color.white;
        }
    }

    // SERVER SEITE: Einsammeln
    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<PlayerInventory>(out PlayerInventory inventory))
        {
            ItemDefinition itemDef = ItemRegistry.GetItem(itemID); // <--- Registry Nutzung
            
            if (itemDef != null)
            {
                inventory.AddItem(itemDef, amount);
                NetworkServer.Destroy(gameObject);
            }
        }
    }
}
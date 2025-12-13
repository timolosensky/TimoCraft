using UnityEngine;
using Mirror;

// Wir brauchen Zugriff auf das Inventar
[RequireComponent(typeof(PlayerInventory))] 
public class PlayerInteraction : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float interactionRange = 8f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform cameraTransform;

    [Header("Visuals")]
    [SerializeField] private GameObject selectionBoxPrefab;
    private GameObject selectionBoxInstance;

    private PlayerInventory playerInventory;

    private void Awake()
    {
        playerInventory = GetComponent<PlayerInventory>();
    }

    private void Start()
    {
        if (isLocalPlayer)
        {
            LockCursor();
            if (selectionBoxPrefab != null)
            {
                selectionBoxInstance = Instantiate(selectionBoxPrefab);
                selectionBoxInstance.SetActive(false);
            }
            
            // Initiales UI Update erzwingen, falls UI schon da ist
            // (Passiert manchmal, wenn Start Reihenfolge variiert)
        }
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        HandleInputFocus();
        if (Cursor.lockState == CursorLockMode.None) return;

        // --- Hinweis: Keine Tastenlogik (1-4) mehr hier, das macht PlayerInventory! ---

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, groundLayer))
        {
            Vector3Int lookPos = Vector3Int.FloorToInt(hit.point - hit.normal * 0.01f);
            
            if (selectionBoxInstance != null)
            {
                selectionBoxInstance.SetActive(true);
                selectionBoxInstance.transform.position = lookPos + new Vector3(0.5f, 0.5f, 0.5f);
            }

            if (Input.GetMouseButtonDown(0)) // Linksklick -> Abbauen
            {
                CmdModifyBlock(lookPos, BlockType.Air);
            }
            else if (Input.GetMouseButtonDown(1)) // Rechtsklick -> Bauen
            {
                Vector3Int buildPos = Vector3Int.FloorToInt(hit.point + hit.normal * 0.01f);
                
                // HIER IST DIE NEUE LOGIK:
                ItemStack currentStack = playerInventory.GetSelectedItem();

                // 1. Haben wir ein Item ausgewählt?
                // 2. Ist das Item ein Block? (Werkzeuge platzieren wir nicht)
                if (currentStack != null && currentStack.item != null && currentStack.item.isBlock)
                {
                    if (!PlayerIsInBlock(buildPos))
                    {
                        CmdModifyBlock(buildPos, currentStack.item.blockType);
                    }
                }
            }
        }
        else
        {
            if (selectionBoxInstance != null) selectionBoxInstance.SetActive(false);
        }
    }
    
    // ... (Restliche Methoden wie HandleInputFocus, LockCursor, CmdModifyBlock bleiben gleich) ...
    private bool PlayerIsInBlock(Vector3Int pos)
    {
        float dist = Vector3.Distance(transform.position, pos + new Vector3(0.5f, 0.5f, 0.5f));
        return dist < 1.3f;
    }

    private void HandleInputFocus()
    {
        if (Input.GetKeyDown(KeyCode.Caret)) UnlockCursor(); 
        else if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None) LockCursor();
    }

    private void LockCursor() { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    private void UnlockCursor() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }

    [Command]
    private void CmdModifyBlock(Vector3Int position, BlockType blockType)
    {
        if (Vector3.Distance(transform.position, position) > interactionRange + 2f) return;
        if (World.Instance != null) World.Instance.ServerSetBlock(position, blockType);
    }
}
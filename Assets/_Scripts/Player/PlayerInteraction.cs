using UnityEngine;
using Mirror;

public class PlayerInteraction : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float interactionRange = 8f; // Reichweite erhöht für die hohen Berge
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform cameraTransform;

    [Header("Visuals")]
    [SerializeField] private GameObject selectionBoxPrefab; // Hier das Prefab reinziehen
    private GameObject selectionBoxInstance;

    // Aktuell ausgewählter Block
    private BlockType currentBlockToPlace = BlockType.Stone;

    private void Start()
    {
        if (isLocalPlayer)
        {
            LockCursor();
            
            // Selection Box nur für den lokalen Spieler instanziieren
            if (selectionBoxPrefab != null)
            {
                selectionBoxInstance = Instantiate(selectionBoxPrefab);
                selectionBoxInstance.SetActive(false); // Erstmal verstecken
            }
        }
    }

    private void Update()
    {
        if (!isLocalPlayer) return;

        // 1. Cursor / Menü Logik
        HandleInputFocus();
        if (Cursor.lockState == CursorLockMode.None) return;

        // 2. Block Auswahl (Primitives Hotbar System)
        HandleBlockSelection();

        // 3. Raycast & Highlighting
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactionRange, groundLayer))
        {
            // --- HIGHLIGHTING ---
            // Wir wollen immer den Block highlighten, den wir abbauen würden (also den getroffenen)
            Vector3Int lookPos = Vector3Int.FloorToInt(hit.point - hit.normal * 0.01f);
            
            if (selectionBoxInstance != null)
            {
                selectionBoxInstance.SetActive(true);
                // Snap to Grid: Position + 0.5f, weil der Pivot vom Cube in der Mitte ist
                selectionBoxInstance.transform.position = lookPos + new Vector3(0.5f, 0.5f, 0.5f);
            }

            // --- INTERAKTION ---
            if (Input.GetMouseButtonDown(0)) // Linksklick -> Abbauen
            {
                // Abbauen passiert an der Koordinate "lookPos" (im Block)
                CmdModifyBlock(lookPos, BlockType.Air);
            }
            else if (Input.GetMouseButtonDown(1)) // Rechtsklick -> Bauen
            {
                // Bauen passiert eins davor (hit.point + normal)
                Vector3Int buildPos = Vector3Int.FloorToInt(hit.point + hit.normal * 0.01f);
                
                // Verhindern, dass man sich selbst einbaut
                if (!PlayerIsInBlock(buildPos))
                {
                    CmdModifyBlock(buildPos, currentBlockToPlace);
                }
            }
        }
        else
        {
            // Nichts getroffen -> Box verstecken
            if (selectionBoxInstance != null) selectionBoxInstance.SetActive(false);
        }
    }

    private void HandleBlockSelection()
    {
        // Einfache Tastenbelegung für Phase 2 Debugging
        if (Input.GetKeyDown(KeyCode.Alpha1)) { currentBlockToPlace = BlockType.Stone; ShowUI("Stone"); }
        if (Input.GetKeyDown(KeyCode.Alpha2)) { currentBlockToPlace = BlockType.Dirt; ShowUI("Dirt"); }
        if (Input.GetKeyDown(KeyCode.Alpha3)) { currentBlockToPlace = BlockType.Grass; ShowUI("Grass"); }
        if (Input.GetKeyDown(KeyCode.Alpha4)) { currentBlockToPlace = BlockType.Bedrock; ShowUI("Bedrock"); }
        // if (Input.GetKeyDown(KeyCode.Alpha5)) ... weitere
    }

    // Kleines Debug-Feedback (später echtes UI)
    private void ShowUI(string blockName)
    {
        Debug.Log($"Selected Block: {blockName}");
    }

    private bool PlayerIsInBlock(Vector3Int pos)
    {
        // Einfacher Check: Ist die Distanz vom Spieler-Pivot zum Block-Mittelpunkt kleiner als Spieler-Radius?
        // Besser: Bounds.Intersects nutzen. Für jetzt reicht Distanz.
        float dist = Vector3.Distance(transform.position, pos + new Vector3(0.5f, 0.5f, 0.5f));
        return dist < 1.3f; // Ungefährer Radius (Spieler ist ca 2 hoch, 0.5 breit)
    }

    private void HandleInputFocus()
    {
        if (Input.GetKeyDown(KeyCode.Caret)) UnlockCursor(); // Taste ^
        else if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None) LockCursor();
    }

    private void LockCursor() { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    private void UnlockCursor() { Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }

    [Command]
    private void CmdModifyBlock(Vector3Int position, BlockType blockType)
    {
        if (Vector3.Distance(transform.position, position) > interactionRange + 2f) return;
        
        if (World.Instance != null)
            World.Instance.ServerSetBlock(position, blockType);
    }
}
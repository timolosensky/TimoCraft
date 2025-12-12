using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

public class PlayerInteraction : NetworkBehaviour
{
    [Header("Einstellungen")]
    public float reachDistance = 5.0f;
    public LayerMask groundLayer;

    void Update()
    {
        if (!isLocalPlayer) return;
        if (Cursor.visible) return;
        if (Camera.main == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryRemoveBlock();
        }
    }

    void TryRemoveBlock()
    {
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, reachDistance, groundLayer))
        {
            // Debug Linie
            Debug.DrawLine(ray.origin, hit.point, Color.red, 1.0f);

            // 1. Welchen Chunk haben wir getroffen?
            // Wir suchen das Skript auf dem getroffenen Objekt
            Chunk hitChunk = hit.collider.GetComponent<Chunk>();

            if (hitChunk == null)
            {
                Debug.LogWarning("Objekt auf Ground-Layer getroffen, aber kein Chunk-Script gefunden!");
                return;
            }

            // 2. Globale Koordinaten des getroffenen Blocks berechnen
            // (Wir gehen ein kleines Stück in den Block hinein)
            Vector3 pointInBlock = hit.point - (hit.normal * 0.01f);
            
            int globalX = Mathf.FloorToInt(pointInBlock.x);
            int globalY = Mathf.FloorToInt(pointInBlock.y);
            int globalZ = Mathf.FloorToInt(pointInBlock.z);

            // 3. Lokale Koordinaten berechnen
            // Wir ziehen die Position des Chunks von der globalen Position ab.
            // WICHTIG: Das setzt voraus, dass deine Chunk-GameObjects auch wirklich
            // an den Positionen (0,0,0), (16,0,0) etc. in der Welt stehen!
            
            int localX = globalX - Mathf.FloorToInt(hitChunk.transform.position.x);
            int localY = globalY - Mathf.FloorToInt(hitChunk.transform.position.y);
            int localZ = globalZ - Mathf.FloorToInt(hitChunk.transform.position.z);

            // Sicherheitscheck mit den Daten aus ChunkData
            if (localX >= 0 && localX < ChunkData.ChunkWidth &&
                localY >= 0 && localY < ChunkData.ChunkHeight &&
                localZ >= 0 && localZ < ChunkData.ChunkWidth)
            {
                // Befehl an den Server senden
                CmdRemoveBlock(hitChunk.gameObject, localX, localY, localZ);
            }
            else
            {
                Debug.LogError($"Berechnete Koordinaten außerhalb des Chunks: {localX}, {localY}, {localZ}");
            }
        }
    }

    // --- NETZWERK ---

    [Command]
    void CmdRemoveBlock(GameObject chunkObject, int x, int y, int z)
    {
        // Der Server sucht sich das Chunk-Skript auf dem übergebenen Objekt
        Chunk chunk = chunkObject.GetComponent<Chunk>();
        
        if (chunk != null)
        {
            // Wir sagen dem Chunk: "Setz Block auf 0 (Luft)"
            chunk.ServerSetBlock(x, y, z, 0);
        }
    }
}
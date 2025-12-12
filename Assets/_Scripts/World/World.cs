using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class World : NetworkBehaviour
{
    // Singleton-Muster für einfachen Zugriff
    public static World Instance { get; private set; }

    [Header("World Settings")]
    [SerializeField] private int chunkWidth = 16;
    [SerializeField] private int chunkHeight = 128;
    
    // Dictionary speichert alle geladenen Chunks via Koordinate (x, z)
    private Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();

    private void Awake()
    {
        // Singleton sicherstellen
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // --- Chunk Management ---

    /// <summary>
    /// Fügt einen Chunk zur Verwaltung hinzu (wird von Chunk.cs beim Start aufgerufen)
    /// </summary>
    public void RegisterChunk(Vector2Int coord, Chunk chunk)
    {
        if (!chunks.ContainsKey(coord))
        {
            chunks.Add(coord, chunk);
        }
    }

    public Chunk GetChunkAt(Vector2Int coord)
    {
        if (chunks.TryGetValue(coord, out Chunk chunk))
        {
            return chunk;
        }
        return null;
    }

    // --- Netzwerk & Interaktion ---

    /// <summary>
    /// Server-Methode: Wird vom PlayerInteraction Skript aufgerufen.
    /// Prüft und verteilt die Änderung an alle Clients.
    /// </summary>
    [Server]
    public void ServerSetBlock(Vector3 worldPosition, BlockType blockType)
    {
        // Optional: Hier Serverseitige Validierung (Darf der Spieler hier bauen?)
        
        // Führe den RPC aus, damit alle (inkl. Host) das Update bekommen
        RpcSetBlock(worldPosition, blockType);
    }

    /// <summary>
    /// RPC: Führt die Änderung auf ALLEN Clients aus.
    /// </summary>
    [ClientRpc]
    private void RpcSetBlock(Vector3 worldPosition, BlockType blockType)
    {
        ModifyVoxelData(worldPosition, blockType);
    }

    /// <summary>
    /// Lokale Logik: Rechnet Weltkoordinaten um, findet den Chunk und setzt den Block.
    /// </summary>
    private void ModifyVoxelData(Vector3 worldPosition, BlockType blockType)
    {
        // 1. Umrechnung Welt-Position -> Voxel-Int-Position
        Vector3Int posInt = Vector3Int.FloorToInt(worldPosition);

        // 2. Chunk Koordinaten berechnen
        int chunkX = Mathf.FloorToInt((float)posInt.x / chunkWidth);
        int chunkZ = Mathf.FloorToInt((float)posInt.z / chunkWidth);
        Vector2Int chunkCoord = new Vector2Int(chunkX, chunkZ);

        // 3. Lokale Position im Chunk berechnen
        int localX = posInt.x - (chunkX * chunkWidth);
        int localZ = posInt.z - (chunkZ * chunkWidth);
        int localY = posInt.y;

        // Sicherheitscheck: Ist Y innerhalb der Höhe?
        if (localY < 0 || localY >= chunkHeight) return;

        // 4. Chunk holen und ändern
        Chunk chunk = GetChunkAt(chunkCoord);
    if (chunk != null)
    {
        // 1. Daten setzen
        chunk.SetBlock(localX, localY, localZ, blockType);
        
        // 2. Mesh SOFORT aktualisieren (wichtig für visuelles Feedback)
        chunk.RegenerateMesh();

        // 3. Nachbarn prüfen (optional, siehe World.cs Skript von vorhin)
        UpdateNeighborChunks(chunkCoord, localX, localZ);
    }
    }
    
    private void UpdateNeighborChunks(Vector2Int centerCoord, int localX, int localZ)
    {
        // Wenn ganz links (x=0), muss der linke Nachbar (x-1) aktualisiert werden
        if (localX == 0) RefreshChunkMesh(centerCoord + Vector2Int.left);
        // Wenn ganz rechts (x=width-1), muss der rechte Nachbar (x+1) aktualisiert werden
        if (localX == chunkWidth - 1) RefreshChunkMesh(centerCoord + Vector2Int.right);
        
        if (localZ == 0) RefreshChunkMesh(centerCoord + Vector2Int.down);
        if (localZ == chunkWidth - 1) RefreshChunkMesh(centerCoord + Vector2Int.up);
    }

    private void RefreshChunkMesh(Vector2Int coord)
    {
        Chunk c = GetChunkAt(coord);
        if (c != null) c.RegenerateMesh(); // Annahme: Methode heißt RegenerateMesh oder UpdateMesh
    }
}
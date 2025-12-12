using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class World : NetworkBehaviour
{
    public static World Instance { get; private set; }

    [Header("World Settings")]
    [SerializeField] private GameObject chunkPrefab; // HIER DAS PREFAB REINZIEHEN
    [SerializeField] private int viewDistance = 5;   // Radius in Chunks (5 = 11x11 Chunks geladen)
    
    // Konstanten aus ChunkData
    private int chunkWidth = ChunkData.ChunkWidth;
    private int chunkHeight = ChunkData.ChunkHeight;

    // Speicherung aller Chunks
    private Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();

    // Tracking
    private Transform playerTransform;
    private Vector2Int lastPlayerChunkCoord;
    private bool isGenerating = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    private void Start()
    {
        // Startet die Routine, um den Spieler zu suchen
        StartCoroutine(FindPlayerRoutine());
    }

    private void Update()
    {
        if (playerTransform == null) return;

        // Berechne aktuelle Chunk-Position des Spielers
        Vector2Int currentChunkCoord = GetChunkCoordFromVector3(playerTransform.position);

        // Wenn der Spieler sich in einen neuen Chunk bewegt hat (oder am Anfang ist)
        if (currentChunkCoord != lastPlayerChunkCoord || chunks.Count == 0)
        {
            lastPlayerChunkCoord = currentChunkCoord;
            UpdateVisibleChunks(currentChunkCoord);
        }
    }

    // --- Chunk Management Logic ---

    private void UpdateVisibleChunks(Vector2Int centerCoord)
    {
        // Wir nutzen eine Coroutine, um nicht 100 Chunks in einem Frame zu erstellen (Lag!)
        if (!isGenerating)
        {
            StartCoroutine(GenerateChunksRoutine(centerCoord));
        }
    }

    private IEnumerator GenerateChunksRoutine(Vector2Int center)
    {
        isGenerating = true;

        List<Vector2Int> activeCoords = new List<Vector2Int>();

        // Spiral-Pattern oder einfache Distanz-Schleife
        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int z = -viewDistance; z <= viewDistance; z++)
            {
                Vector2Int offset = new Vector2Int(x, z);
                Vector2Int coord = center + offset;
                activeCoords.Add(coord);

                if (!chunks.ContainsKey(coord))
                {
                    CreateChunk(coord);
                    
                    // PERFORMANCE FIX:
                    // Warte NACH JEDEM einzelnen Chunk einen Frame.
                    // Das verteilt die Last drastisch besser.
                    yield return null; 
                }
            }
        }

        // Cleanup Logic (bleibt gleich)
        List<Vector2Int> loadedChunks = new List<Vector2Int>(chunks.Keys);
        foreach (Vector2Int coord in loadedChunks)
        {
            if (!activeCoords.Contains(coord))
            {
                DestroyChunk(coord);
                // Auch beim Zerstören kurz warten, um Ruckler zu vermeiden
                if (loadedChunks.IndexOf(coord) % 5 == 0) yield return null;
            }
        }

        isGenerating = false;
    }

    private void CreateChunk(Vector2Int coord)
    {
        // Instanziieren
        GameObject newChunkObj = Instantiate(chunkPrefab, new Vector3(coord.x * chunkWidth, 0, coord.y * chunkWidth), Quaternion.identity);
        newChunkObj.name = $"Chunk {coord.x}_{coord.y}";
        
        // Chunk Script Setup
        Chunk newChunk = newChunkObj.GetComponent<Chunk>();
        newChunk.chunkPosition = coord;
        
        // In Dictionary eintragen (bevor Start() im Chunk aufgerufen wird, registriert er sich normalerweise selbst, 
        // aber wir machen es hier explizit für saubere Kontrolle)
        chunks.Add(coord, newChunk);
        
        // Parent setzen für Ordnung in der Hierarchy
        newChunkObj.transform.SetParent(this.transform);
    }

    private void DestroyChunk(Vector2Int coord)
    {
        if (chunks.TryGetValue(coord, out Chunk chunk))
        {
            Destroy(chunk.gameObject);
            chunks.Remove(coord);
        }
    }

    // --- Helper & Network ---

    private IEnumerator FindPlayerRoutine()
    {
        // Wartet, bis der LocalPlayer gespawnt ist
        while (playerTransform == null)
        {
            if (NetworkClient.localPlayer != null)
            {
                playerTransform = NetworkClient.localPlayer.transform;
            }
            yield return null;
        }
        
        // Initiales Laden erzwingen
        UpdateVisibleChunks(GetChunkCoordFromVector3(playerTransform.position));
    }

    public Vector2Int GetChunkCoordFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / chunkWidth);
        int z = Mathf.FloorToInt(pos.z / chunkWidth);
        return new Vector2Int(x, z);
    }
    
    public Chunk GetChunkAt(Vector2Int coord)
    {
        if (chunks.TryGetValue(coord, out Chunk chunk)) return chunk;
        return null;
    }

    // --- Network Commands (Server Authoritative Building) ---

    [Server]
    public void ServerSetBlock(Vector3 worldPosition, BlockType blockType)
    {
        RpcSetBlock(worldPosition, blockType);
    }

    [ClientRpc]
    private void RpcSetBlock(Vector3 worldPosition, BlockType blockType)
    {
        // Gleiche Logik wie zuvor, aber nutzt jetzt die helper Methoden
        Vector3Int posInt = Vector3Int.FloorToInt(worldPosition);
        Vector2Int chunkCoord = GetChunkCoordFromVector3(worldPosition);
        
        Chunk chunk = GetChunkAt(chunkCoord);
        if (chunk != null)
        {
            // Lokale Koordinaten im Chunk berechnen
            int localX = posInt.x - (chunkCoord.x * chunkWidth);
            int localZ = posInt.z - (chunkCoord.y * chunkWidth);
            int localY = posInt.y; // Y bleibt absolut in ChunkData logic, aber prüfen wir:

            // Achtung: Mathe bei negativen Koordinaten korrigieren für lokale Pos
            // Wenn worldX = -1, chunkX = -1. localX muss 15 sein (bei width 16).
            localX = Mathf.Abs(localX) % chunkWidth; // Vereinfacht, korrekte Modulo Math ist komplexer in C#
            // Bessere Modulo Funktion für C# (da % bei negativen Zahlen negativ bleibt):
            localX = (posInt.x % chunkWidth + chunkWidth) % chunkWidth;
            localZ = (posInt.z % chunkWidth + chunkWidth) % chunkWidth;

            chunk.SetBlock(localX, localY, localZ, blockType);
            chunk.RegenerateMesh();
        }
    }
}
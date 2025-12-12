using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics; // WICHTIG: Stelle sicher, dass das Package installiert ist

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    // --- Einstellungen ---
    [Header("Generation Settings")]
    public Vector2Int chunkPosition; // Die Koordinate dieses Chunks (z.B. 0,0 oder 1,0)

    // --- Noise & Layer Konfiguration ---
    [Header("Terrain Layers")]
    [SerializeField] private int baseHeight = 140;      
    [SerializeField] private float mountainScale = 0.005f; 
    [SerializeField] private float mountainHeight = 180f;  

    // Konstanten für deine Welt-Schichten
    private const int LAYER_BEDROCK = 2;
    private const int LAYER_LAVA_CAVES_START = 3;
    private const int LAYER_LAVA_CAVES_END = 78;
    private const int LAYER_DEEP_CAVES_START = 79;
    private const int LAYER_DEEP_CAVES_END = 179;
    private const int SEA_LEVEL = 190;
    private const int MOUNTAIN_PEAK = 350;

    // --- Interne Daten ---
    private ChunkData chunkData; // Unsere Datenklasse (reines C#, kein GameObject)

    // Listen für das Mesh-Building
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector3> uvs = new List<Vector3>();

    private Mesh mesh;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        mesh = new Mesh();
        
        // Hier wird die Datenklasse im Speicher erstellt
        chunkData = new ChunkData();
    }

    void Start()
    {
        // 1. Sich beim World-Manager anmelden (falls vorhanden)
        if (World.Instance != null)
        {
            World.Instance.RegisterChunk(chunkPosition, this);
        }

        // 2. Welt generieren
        GenerateTerrainData();

        // 3. Sichtbar machen
        RegenerateMesh();
    }

    // --- API für den World Manager ---

    public void SetBlock(int x, int y, int z, BlockType type)
    {
        // Speichert den Block in den Daten (noch nicht sichtbar)
        chunkData.SetBlock(x, y, z, (byte)type);
    }

    // Baut das visuelle 3D-Modell neu (muss nach SetBlock aufgerufen werden, um Änderungen zu sehen)
    public void RegenerateMesh()
    {
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();

        for (int y = 0; y < ChunkData.ChunkHeight; y++)
        {
            for (int x = 0; x < ChunkData.ChunkWidth; x++)
            {
                for (int z = 0; z < ChunkData.ChunkWidth; z++)
                {
                    byte blockID = chunkData.GetBlock(x, y, z);
                    if (blockID == 0) continue; // Luft überspringen

                    // Face Culling: Nur Seiten zeichnen, die an Luft grenzen
                    if (CheckTransparent(x + 1, y, z)) AddFace(Vector3.right, x, y, z, blockID);
                    if (CheckTransparent(x - 1, y, z)) AddFace(Vector3.left, x, y, z, blockID);
                    if (CheckTransparent(x, y + 1, z)) AddFace(Vector3.up, x, y, z, blockID);
                    if (CheckTransparent(x, y - 1, z)) AddFace(Vector3.down, x, y, z, blockID);
                    if (CheckTransparent(x, y, z + 1)) AddFace(Vector3.forward, x, y, z, blockID);
                    if (CheckTransparent(x, y, z - 1)) AddFace(Vector3.back, x, y, z, blockID);
                }
            }
        }

        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh; // Physik aktualisieren
    }

    // --- Terrain Generation Logik (Der komplexe Teil) ---

    void GenerateTerrainData()
    {
        // Absolute Position für Noise berechnen
        int3 chunkPos = new int3(chunkPosition.x, 0, chunkPosition.y) * ChunkData.ChunkWidth;

        for (int x = 0; x < ChunkData.ChunkWidth; x++)
        {
            for (int z = 0; z < ChunkData.ChunkWidth; z++)
            {
                float worldX = chunkPos.x + x;
                float worldZ = chunkPos.z + z;

                // 1. OBERFLÄCHEN-BERECHNUNG
                float biomeNoise = noise.cnoise(new float2(worldX, worldZ) * 0.002f);
                float mountainFactor = math.remap(-1f, 1f, 0f, 1f, biomeNoise);
                
                float detailNoise = noise.cnoise(new float2(worldX, worldZ) * 0.01f);
                
                // Höhe zwischen Meer (190) und Bergspitze (350) interpolieren
                float terrainHeightFloat = math.lerp(
                    SEA_LEVEL + 5 + (detailNoise * 10), 
                    SEA_LEVEL + (detailNoise * 20) + (mountainFactor * (MOUNTAIN_PEAK - SEA_LEVEL)), 
                    mountainFactor * mountainFactor 
                );
                
                int surfaceHeight = (int)terrainHeightFloat;

                // 2. VERTIKALE SCHLEIFE (Von unten nach oben füllen)
                for (int y = 0; y < ChunkData.ChunkHeight; y++)
                {
                    // -- SCHICHT 1: BEDROCK --
                    if (y < LAYER_BEDROCK)
                    {
                        chunkData.SetBlock(x, y, z, (byte)BlockType.Bedrock);
                        continue;
                    }

                    // Standard-Block bestimmen (falls keine Höhle)
                    BlockType currentBlock = BlockType.Air;

                    if (y <= surfaceHeight)
                    {
                        if (y < LAYER_DEEP_CAVES_START) currentBlock = BlockType.Stone;
                        else if (y < surfaceHeight - 3) currentBlock = BlockType.Stone;
                        else if (y < surfaceHeight) currentBlock = BlockType.Dirt;
                        else currentBlock = BlockType.Grass;
                    }

                    // -- SCHICHT 2: LAVA HÖHLEN (Chaotisch) --
                    if (y >= LAYER_LAVA_CAVES_START && y <= LAYER_LAVA_CAVES_END)
                    {
                        float caveNoise = noise.cnoise(new float3(worldX, y * 1.2f, worldZ) * 0.06f);
                        if (caveNoise > 0.4f) currentBlock = BlockType.Air;
                    }

                    // -- SCHICHT 3: TIEFENHÖHLEN (Riesig) --
                    else if (y >= LAYER_DEEP_CAVES_START && y <= LAYER_DEEP_CAVES_END)
                    {
                        float deepCaveNoise = noise.cnoise(new float3(worldX, y * 0.5f, worldZ) * 0.015f);
                        if (deepCaveNoise > 0.2f) currentBlock = BlockType.Air;
                    }

                    // -- SCHICHT 4: OBERFLÄCHEN-EINGÄNGE --
                    else if (y > LAYER_DEEP_CAVES_END && y <= surfaceHeight)
                    {
                        float surfaceCaveNoise = noise.cnoise(new float3(worldX, y, worldZ) * 0.04f);
                        if (surfaceCaveNoise > 0.5f) currentBlock = BlockType.Air;
                    }

                    // Block final setzen
                    chunkData.SetBlock(x, y, z, (byte)currentBlock);
                }
            }
        }
    }

    // --- Helper Funktionen ---

    bool CheckTransparent(int x, int y, int z)
    {
        if (!chunkData.IsValid(x, y, z)) return true; // Chunk-Rand immer zeichnen
        return chunkData.GetBlock(x, y, z) == 0;
    }

    int GetTextureID(byte blockID, Vector3 direction)
    {
        // Wir nehmen an, dein Texture Array ist so sortiert:
        // Index 0: Erde (Dirt)
        // Index 1: Gras Oben (Grass Top)
        // Index 2: Stein (Stone)
        // Index 3: Bedrock (falls vorhanden, sonst Stein)

        // --- GRAS BLOCK ---
        if (blockID == (byte)BlockType.Grass) 
        {
            if (direction == Vector3.up)
            {
                return 1; // Oben = Gras Textur
            }
            // Alle anderen Seiten (Unten, Links, Rechts...) = Erde
            // Das fixt dein Problem, dass die Seiten nach Stein aussehen.
            return 0; 
        }

        // --- ERDE ---
        if (blockID == (byte)BlockType.Dirt) return 0; 

        // --- STEIN ---
        if (blockID == (byte)BlockType.Stone) return 2; 

        // --- BEDROCK ---
        // Falls du keine Bedrock Textur hast, nehmen wir Stein (2)
        if (blockID == (byte)BlockType.Bedrock) return 2; 

        // Fallback für alles Unbekannte
        return 0; 
    }

    void AddFace(Vector3 dir, int x, int y, int z, byte blockID)
    {
        int vCount = vertices.Count;
        Vector3 start = new Vector3(x, y, z);

        // Quad Vertices basierend auf Richtung
        if (dir == Vector3.up) {
            vertices.Add(start + new Vector3(0, 1, 0)); vertices.Add(start + new Vector3(0, 1, 1));
            vertices.Add(start + new Vector3(1, 1, 1)); vertices.Add(start + new Vector3(1, 1, 0));
        } else if (dir == Vector3.down) {
             vertices.Add(start + new Vector3(0, 0, 1)); vertices.Add(start + new Vector3(0, 0, 0));
             vertices.Add(start + new Vector3(1, 0, 0)); vertices.Add(start + new Vector3(1, 0, 1));
        } else if (dir == Vector3.right) {
             vertices.Add(start + new Vector3(1, 0, 0)); vertices.Add(start + new Vector3(1, 1, 0));
             vertices.Add(start + new Vector3(1, 1, 1)); vertices.Add(start + new Vector3(1, 0, 1));
        } else if (dir == Vector3.left) {
             vertices.Add(start + new Vector3(0, 0, 1)); vertices.Add(start + new Vector3(0, 1, 1));
             vertices.Add(start + new Vector3(0, 1, 0)); vertices.Add(start + new Vector3(0, 0, 0));
        } else if (dir == Vector3.forward) {
             vertices.Add(start + new Vector3(1, 0, 1)); vertices.Add(start + new Vector3(1, 1, 1));
             vertices.Add(start + new Vector3(0, 1, 1)); vertices.Add(start + new Vector3(0, 0, 1));
        } else if (dir == Vector3.back) {
             vertices.Add(start + new Vector3(0, 0, 0)); vertices.Add(start + new Vector3(0, 1, 0));
             vertices.Add(start + new Vector3(1, 1, 0)); vertices.Add(start + new Vector3(1, 0, 0));
        }

        // Triangles
        triangles.Add(vCount + 0); triangles.Add(vCount + 1); triangles.Add(vCount + 2);
        triangles.Add(vCount + 0); triangles.Add(vCount + 2); triangles.Add(vCount + 3);

        // UVs (Textur ID in die Z-Komponente für das Texture Array)
        int texID = GetTextureID(blockID, dir);
        uvs.Add(new Vector3(0, 0, texID)); uvs.Add(new Vector3(0, 1, texID));
        uvs.Add(new Vector3(1, 1, texID)); uvs.Add(new Vector3(1, 0, texID));
    }
}
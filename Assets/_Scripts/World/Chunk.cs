using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics; // WICHTIG: Package muss installiert sein

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    // --- Einstellungen ---
    [Header("Data")]
    public Vector2Int chunkPosition; // Koordinate im Grid (z.B. 5, -2)

    // --- Layer Konfiguration (Optimiert für 256 Höhe) ---
    [Header("Terrain Layers (Height 256)")]
    private const int LAYER_BEDROCK = 2;
    private const int LAYER_LAVA_CAVES_START = 3;
    private const int LAYER_LAVA_CAVES_END = 40; 
    private const int LAYER_DEEP_CAVES_START = 41;
    private const int LAYER_DEEP_CAVES_END = 100;
    private const int SEA_LEVEL = 110;
    private const int MOUNTAIN_PEAK = 220; 

    // --- Interne Daten ---
    private ChunkData chunkData; // Unsere Datenklasse

    // Mesh Listen (wiederverwendet für weniger Garbage Collection)
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
        
        chunkData = new ChunkData();
    }

    void Start()
    {
        // Wir registrieren uns NICHT mehr selbst bei World.Instance.
        // World.cs instanziiert uns und registriert uns direkt.
        
        GenerateTerrainData();
        RegenerateMesh();
    }

    // --- API (Wird von World.cs aufgerufen) ---

    public void SetBlock(int x, int y, int z, BlockType type)
    {
        chunkData.SetBlock(x, y, z, (byte)type);
    }

    // WICHTIG FÜR DROPS: Gibt die ID des Blocks zurück (z.B. um zu prüfen, was abgebaut wurde)
    public byte GetBlockByte(int x, int y, int z)
    {
        return chunkData.GetBlock(x, y, z);
    }

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
                    if (blockID == 0) continue; // Luft

                    // Face Culling (Nur sichtbare Seiten zeichnen)
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
        meshCollider.sharedMesh = mesh; // Physik Update
    }

    // --- Terrain Generation ---

    void GenerateTerrainData()
    {
        int3 chunkPos = new int3(chunkPosition.x, 0, chunkPosition.y) * ChunkData.ChunkWidth;

        for (int x = 0; x < ChunkData.ChunkWidth; x++)
        {
            for (int z = 0; z < ChunkData.ChunkWidth; z++)
            {
                float worldX = chunkPos.x + x;
                float worldZ = chunkPos.z + z;

                // 1. OBERFLÄCHEN-HÖHE BERECHNEN
                float biomeNoise = noise.cnoise(new float2(worldX, worldZ) * 0.002f);
                float mountainFactor = math.remap(-1f, 1f, 0f, 1f, biomeNoise);
                
                float detailNoise = noise.cnoise(new float2(worldX, worldZ) * 0.01f);
                
                float terrainHeightFloat = math.lerp(
                    SEA_LEVEL + 5 + (detailNoise * 8), 
                    SEA_LEVEL + (detailNoise * 15) + (mountainFactor * (MOUNTAIN_PEAK - SEA_LEVEL)), 
                    mountainFactor * mountainFactor 
                );
                
                int surfaceHeight = (int)terrainHeightFloat;

                // 2. VERTIKALE SCHLEIFE
                for (int y = 0; y < ChunkData.ChunkHeight; y++)
                {
                    // Layer 1: Bedrock
                    if (y < LAYER_BEDROCK)
                    {
                        chunkData.SetBlock(x, y, z, (byte)BlockType.Bedrock);
                        continue;
                    }

                    BlockType currentBlock = BlockType.Air;

                    // Standard Boden füllen
                    if (y <= surfaceHeight)
                    {
                        if (y < LAYER_DEEP_CAVES_START) currentBlock = BlockType.Stone;
                        else if (y < surfaceHeight - 3) currentBlock = BlockType.Stone;
                        else if (y < surfaceHeight) currentBlock = BlockType.Dirt;
                        else currentBlock = BlockType.Grass;
                    }

                    // Layer 2: Lava Höhlen (Unten)
                    if (y >= LAYER_LAVA_CAVES_START && y <= LAYER_LAVA_CAVES_END)
                    {
                        float caveNoise = noise.cnoise(new float3(worldX, y * 1.5f, worldZ) * 0.08f);
                        if (caveNoise > 0.5f) currentBlock = BlockType.Air;
                    }

                    // Layer 3: Deep Dark (Mitte, riesige Hohlräume)
                    else if (y >= LAYER_DEEP_CAVES_START && y <= LAYER_DEEP_CAVES_END)
                    {
                        float deepCaveNoise = noise.cnoise(new float3(worldX, y * 0.8f, worldZ) * 0.02f);
                        if (deepCaveNoise > 0.25f) currentBlock = BlockType.Air;
                    }

                    // Layer 4: Oberflächen-Eingänge
                    else if (y > LAYER_DEEP_CAVES_END && y <= surfaceHeight)
                    {
                        float surfaceCaveNoise = noise.cnoise(new float3(worldX, y, worldZ) * 0.05f);
                        if (surfaceCaveNoise > 0.5f) currentBlock = BlockType.Air;
                    }

                    chunkData.SetBlock(x, y, z, (byte)currentBlock);
                }
            }
        }
    }

    // --- Mesh Helper ---

    bool CheckTransparent(int x, int y, int z)
    {
        if (!chunkData.IsValid(x, y, z)) return true; // Chunk-Grenze immer zeichnen
        return chunkData.GetBlock(x, y, z) == 0;
    }

    int GetTextureID(byte blockID, Vector3 direction)
    {
        // Textur Index Mapping
        // 0: Dirt, 1: Grass Top, 2: Stone, 3: Bedrock (falls vorhanden, sonst Stone)

        if (blockID == (byte)BlockType.Grass) 
        {
            if (direction == Vector3.up) return 1; // Oben = Grün
            return 0; // Seite/Unten = Erde
        }
        
        if (blockID == (byte)BlockType.Dirt) return 0; 
        if (blockID == (byte)BlockType.Stone) return 2; 
        if (blockID == (byte)BlockType.Bedrock) return 3; // Ggf. Index 3 wenn Bedrock Textur da ist

        return 0; 
    }

    void AddFace(Vector3 dir, int x, int y, int z, byte blockID)
    {
        int vCount = vertices.Count;
        Vector3 start = new Vector3(x, y, z);

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

        triangles.Add(vCount + 0); triangles.Add(vCount + 1); triangles.Add(vCount + 2);
        triangles.Add(vCount + 0); triangles.Add(vCount + 2); triangles.Add(vCount + 3);

        int texID = GetTextureID(blockID, dir);
        uvs.Add(new Vector3(0, 0, texID)); uvs.Add(new Vector3(0, 1, texID));
        uvs.Add(new Vector3(1, 1, texID)); uvs.Add(new Vector3(1, 0, texID));
    }
}
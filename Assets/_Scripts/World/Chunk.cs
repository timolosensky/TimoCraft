using UnityEngine;
using System.Collections.Generic;
using Mirror; // Wir bereiten es schon mal für Multiplayer vor

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
[RequireComponent(typeof(NetworkIdentity))] // Wichtig für Mirror
public class Chunk : NetworkBehaviour
{
    // --- Einstellungen ---
    [Header("Generation Settings")]
    public int chunkPositionX = 0;
    public int chunkPositionZ = 0;
    
    // Wir nutzen unsere optimierte Datenklasse
    private ChunkData chunkData;

    // Listen für das Mesh-Building (wiederverwendet, um Garbage Collection zu sparen)
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
        
        // Daten initialisieren
        chunkData = new ChunkData();
    }

    void Start()
    {
        // Vorerst generieren wir die Welt lokal auf jedem Client (Determinismus)
        // Später sendet der Server den Seed oder die Änderungen.
        GenerateTerrainData();
        UpdateChunkMesh();
    }

    // 1. DATEN: Füllt das Byte-Array mit Leben (Perlin Noise)
    void GenerateTerrainData()
    {
        for (int x = 0; x < ChunkData.ChunkWidth; x++)
        {
            for (int z = 0; z < ChunkData.ChunkWidth; z++)
            {
                // Welt-Koordinaten berechnen
                float worldX = x + (chunkPositionX * ChunkData.ChunkWidth);
                float worldZ = z + (chunkPositionZ * ChunkData.ChunkWidth);

                // Einfacher Noise für Hügel (Höhe 0 bis 30)
                float noise = Mathf.PerlinNoise(worldX * 0.05f, worldZ * 0.05f);
                int terrainHeight = Mathf.FloorToInt(noise * 30) + 10; // +10 damit wir nicht im Wasser starten

                for (int y = 0; y < ChunkData.ChunkHeight; y++)
                {
                    if (y == 0) 
                    {
                        chunkData.SetBlock(x, y, z, 4); // Bedrock (ID 4)
                    }
                    else if (y < terrainHeight)
                    {
                        chunkData.SetBlock(x, y, z, 3); // Stein/Erde (ID 3)
                    }
                    else if (y == terrainHeight)
                    {
                        chunkData.SetBlock(x, y, z, 1); // Gras (ID 1)
                    }
                    else
                    {
                        chunkData.SetBlock(x, y, z, 0); // Luft
                    }
                }
            }
        }
    }

    // 2. GRAFIK: Baut das Mesh basierend auf den Daten

    int GetTextureID(byte blockID, Vector3 direction)
        {
            // MERKE: Diese Indizes müssen zu deinem Array aus Schritt 2 passen!

            // Gras (ID 1)
            if (blockID == 1) {
                if (direction == Vector3.up) return 1; // Gras Oben
                if (direction == Vector3.down) return 0; // Erde Unten
                return 0; // Erde Seite (oder Gras Seite, wenn du hast)
            }
            // Erde (ID 2 - falls du ID 2 nutzt)
            if (blockID == 2) return 0; 

            // Stein (ID 3)
            if (blockID == 3) return 2; // Stein Textur

            return 0; // Fallback
        }


    public void UpdateChunkMesh()
    {
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();

        // Schleife durch alle Blöcke
        for (int y = 0; y < ChunkData.ChunkHeight; y++)
        {
            for (int x = 0; x < ChunkData.ChunkWidth; x++)
            {
                for (int z = 0; z < ChunkData.ChunkWidth; z++)
                {
                    byte blockID = chunkData.GetBlock(x, y, z);
                    if (blockID == 0) continue; // Luft wird nicht gezeichnet

                    // FACE CULLING: Prüfe Nachbarn. Zeichne nur, wenn Nachbar transparent ist.
                    
                    // Rechts (X + 1)
                    if (CheckTransparent(x + 1, y, z)) AddFace(new Vector3(1, 0, 0), x, y, z, blockID);
                    // Links (X - 1)
                    if (CheckTransparent(x - 1, y, z)) AddFace(new Vector3(-1, 0, 0), x, y, z, blockID);
                    // Oben (Y + 1)
                    if (CheckTransparent(x, y + 1, z)) AddFace(new Vector3(0, 1, 0), x, y, z, blockID);
                    // Unten (Y - 1)
                    if (CheckTransparent(x, y - 1, z)) AddFace(new Vector3(0, -1, 0), x, y, z, blockID);
                    // Vorne (Z + 1)
                    if (CheckTransparent(x, y, z + 1)) AddFace(new Vector3(0, 0, 1), x, y, z, blockID);
                    // Hinten (Z - 1)
                    if (CheckTransparent(x, y, z - 1)) AddFace(new Vector3(0, 0, -1), x, y, z, blockID);
                }
            }
        }

        // Mesh an Unity übergeben
        mesh.Clear();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.SetUVs(0, uvs);
        
        mesh.RecalculateNormals(); // Wichtig für Licht/Schatten

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh; // Physik aktivieren
    }

    // Hilfsfunktion: Ist der Block an dieser Stelle durchsichtig (oder außerhalb des Chunks)?
    bool CheckTransparent(int x, int y, int z)
    {
        // Wenn Koordinate außerhalb des Chunks liegt:
        // Für den Anfang sagen wir "Ja, zeichne die Kante", damit der Chunk am Rand nicht offen ist.
        if (!chunkData.IsValid(x, y, z)) return true;

        byte id = chunkData.GetBlock(x, y, z);
        return id == 0; // 0 = Luft
    }

    void AddFace(Vector3 dir, int x, int y, int z, byte blockID)
    {
        int vCount = vertices.Count;
        Vector3 start = new Vector3(x, y, z);

        // Geometrie für die 6 Seiten (Hardcoded für Performance)
        if (dir == Vector3.up) {
            vertices.Add(start + new Vector3(0, 1, 0));
            vertices.Add(start + new Vector3(0, 1, 1));
            vertices.Add(start + new Vector3(1, 1, 1));
            vertices.Add(start + new Vector3(1, 1, 0));
        } else if (dir == Vector3.down) {
             vertices.Add(start + new Vector3(0, 0, 1));
             vertices.Add(start + new Vector3(0, 0, 0));
             vertices.Add(start + new Vector3(1, 0, 0));
             vertices.Add(start + new Vector3(1, 0, 1));
        } else if (dir == Vector3.right) {
             vertices.Add(start + new Vector3(1, 0, 0));
             vertices.Add(start + new Vector3(1, 1, 0));
             vertices.Add(start + new Vector3(1, 1, 1));
             vertices.Add(start + new Vector3(1, 0, 1));
        } else if (dir == Vector3.left) {
             vertices.Add(start + new Vector3(0, 0, 1));
             vertices.Add(start + new Vector3(0, 1, 1));
             vertices.Add(start + new Vector3(0, 1, 0));
             vertices.Add(start + new Vector3(0, 0, 0));
        } else if (dir == Vector3.forward) {
             vertices.Add(start + new Vector3(1, 0, 1));
             vertices.Add(start + new Vector3(1, 1, 1));
             vertices.Add(start + new Vector3(0, 1, 1));
             vertices.Add(start + new Vector3(0, 0, 1));
        } else if (dir == Vector3.back) {
             vertices.Add(start + new Vector3(0, 0, 0));
             vertices.Add(start + new Vector3(0, 1, 0));
             vertices.Add(start + new Vector3(1, 1, 0));
             vertices.Add(start + new Vector3(1, 0, 0));
        }

        // Triangles (zwei Dreiecke pro Fläche)
        triangles.Add(vCount + 0);
        triangles.Add(vCount + 1);
        triangles.Add(vCount + 2);
        triangles.Add(vCount + 0);
        triangles.Add(vCount + 2);
        triangles.Add(vCount + 3);

        int texID = GetTextureID(blockID, dir);

        // Vector3 statt Vector2! Die 3. Zahl ist der Index.
        uvs.Add(new Vector3(0, 0, texID));
        uvs.Add(new Vector3(0, 1, texID));
        uvs.Add(new Vector3(1, 1, texID));
        uvs.Add(new Vector3(1, 0, texID));
    }
    
    // --- NETZWERK LOGIK ---

    // Diese Methode wird vom Server aufgerufen (aus PlayerInteraction)
    [Server]
    public void ServerSetBlock(int localX, int localY, int localZ, byte newBlockID)
    {
        // 1. Auf dem Server ändern (damit Physik/Collider stimmt)
        chunkData.SetBlock(localX, localY, localZ, newBlockID);
        UpdateChunkMesh();

        // 2. An ALLE Clients weitersagen
        RpcUpdateBlock(localX, localY, localZ, newBlockID);
    }

    // ClientRpc: Läuft auf ALLEN Clients (auch beim Host)
    [ClientRpc]
    void RpcUpdateBlock(int localX, int localY, int localZ, byte newBlockID)
    {
        // Wir prüfen, ob wir der Host sind. 
        // Der Host hat es oben in ServerSetBlock schon gemacht, 
        // also muss er es nicht doppelt machen (spart Performance).
        if (isServer) return; 

        // 1. Daten lokal ändern
        chunkData.SetBlock(localX, localY, localZ, newBlockID);

        // 2. Mesh neu bauen (damit das Loch sichtbar wird)
        UpdateChunkMesh();
    }





}
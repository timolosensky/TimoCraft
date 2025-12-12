using UnityEngine;

// Diese Klasse speichert NUR die rohen Zahlen (IDs). Keine Grafik.
public class ChunkData
{
    // Die fixen Größen. const ist extrem schnell.
    public const int ChunkWidth = 16;
    public const int ChunkHeight = 384; // Unsere riesige Höhe

    // Das flache Array (6MB RAM sparen vs. Objekte)
    private byte[] blocks;

    public ChunkData()
    {
        // Erstellt das Array einmalig im Speicher (ca. 98 KB pro Chunk)
        blocks = new byte[ChunkWidth * ChunkHeight * ChunkWidth];
    }

    // Die magische Formel: 3D Koordinate -> 1D Index
    public int GetIndex(int x, int y, int z)
    {
        return x + (z * ChunkWidth) + (y * ChunkWidth * ChunkWidth);
    }

    // Ist die Koordinate überhaupt im Chunk? (Anti-Crash)
    public bool IsValid(int x, int y, int z)
    {
        return x >= 0 && x < ChunkWidth &&
               y >= 0 && y < ChunkHeight &&
               z >= 0 && z < ChunkWidth;
    }

    public byte GetBlock(int x, int y, int z)
    {
        if (!IsValid(x, y, z)) return 0; // 0 = Luft/Fehler
        return blocks[GetIndex(x, y, z)];
    }

    public void SetBlock(int x, int y, int z, byte id)
    {
        if (!IsValid(x, y, z)) return;
        blocks[GetIndex(x, y, z)] = id;
    }
}
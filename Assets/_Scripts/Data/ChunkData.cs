using UnityEngine;

public class ChunkData
{
    public const int ChunkWidth = 16;
    
    public const int ChunkHeight = 256; 

    private byte[] blocks;

    public ChunkData()
    {
        blocks = new byte[ChunkWidth * ChunkHeight * ChunkWidth];
    }
    
    public int GetIndex(int x, int y, int z)
    {
        return x + (z * ChunkWidth) + (y * ChunkWidth * ChunkWidth);
    }

    public bool IsValid(int x, int y, int z)
    {
        return x >= 0 && x < ChunkWidth &&
               y >= 0 && y < ChunkHeight &&
               z >= 0 && z < ChunkWidth;
    }

    public byte GetBlock(int x, int y, int z)
    {
        if (!IsValid(x, y, z)) return 0;
        return blocks[GetIndex(x, y, z)];
    }

    public void SetBlock(int x, int y, int z, byte id)
    {
        if (!IsValid(x, y, z)) return;
        blocks[GetIndex(x, y, z)] = id;
    }
}
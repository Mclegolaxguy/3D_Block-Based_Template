using UnityEngine;

public class Chunk
{
    public const int Size = 16;

    public Vector2Int ChunkCoord;

    public Block[,,] Blocks =
        new Block[Size, Size, Size];

    public Chunk(Vector2Int coord)
    {
        ChunkCoord = coord;
    }

    public Block GetBlock(int x, int y, int z)
    {
        return Blocks[x, y, z];
    }

    public void SetBlock(int x, int y, int z, Block block)
    {
        Blocks[x, y, z] = block;
    }
}
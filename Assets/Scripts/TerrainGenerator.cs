using UnityEngine;

public static class TerrainGenerator
{
    public static void Generate(Chunk chunk)
    {
        for(int x=0;x<Chunk.Size;x++)
        {
            for(int z=0;z<Chunk.Size;z++)
            {
                int worldX =
                    chunk.ChunkCoord.x *
                    Chunk.Size + x;

                int worldZ =
                    chunk.ChunkCoord.y *
                    Chunk.Size + z;

                float noise =
                    Mathf.PerlinNoise(
                        worldX * 0.05f,
                        worldZ * 0.05f
                    );

                int height =
                    Mathf.FloorToInt(
                        noise * 12f
                    );

                for(int y=0;y<Chunk.Size;y++)
                {
                    if(y <= height)
                        chunk.SetBlock(
                            x,y,z,
                            new Block(BlockType.Stone)
                        );
                    else
                        chunk.SetBlock(
                            x,y,z,
                            new Block(BlockType.Air)
                        );
                }
            }
        }
    }
}
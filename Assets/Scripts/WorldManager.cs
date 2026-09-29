using UnityEngine;
using System.Collections.Generic;

public class WorldManager : MonoBehaviour
{
    public GameObject chunkPrefab;

    public Transform player;

    public int renderDistance = 4;

    Dictionary<Vector2Int, ChunkRenderer>
        loadedChunks = new();

    void Start()
    {
        UpdateChunks();
    }

    void Update()
    {
        UpdateChunks();
    }

    void UpdateChunks()
    {
        Vector2Int playerChunk =
            GetChunkCoord(
                player.position
            );

        for(int x=-renderDistance;
            x<=renderDistance;
            x++)
        {
            for(int z=-renderDistance;
                z<=renderDistance;
                z++)
            {
                Vector2Int coord =
                    playerChunk +
                    new Vector2Int(x,z);

                if(!loadedChunks.ContainsKey(coord))
                {
                    CreateChunk(coord);
                }
            }
        }
    }

    Vector2Int GetChunkCoord(Vector3 pos)
    {
        return new Vector2Int(
            Mathf.FloorToInt(
                pos.x / Chunk.Size),
            Mathf.FloorToInt(
                pos.z / Chunk.Size)
        );
    }

    void CreateChunk(Vector2Int coord)
    {
        GameObject chunkObj =
            Instantiate(chunkPrefab);

        chunkObj.name =
            $"Chunk_{coord.x}_{coord.y}";

        chunkObj.transform.position =
            new Vector3(
                coord.x * Chunk.Size,
                0,
                coord.y * Chunk.Size
            );

        Chunk chunk = new Chunk(coord);

        TerrainGenerator.Generate(chunk);

        ChunkRenderer renderer =
            chunkObj.GetComponent<ChunkRenderer>();

        renderer.ChunkData = chunk;

        renderer.BuildChunk();

        loadedChunks.Add(coord, renderer);
    }
}
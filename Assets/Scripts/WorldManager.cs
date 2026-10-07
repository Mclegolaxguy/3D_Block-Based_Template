using System.Collections.Generic;
using UnityEngine;

public class WorldManager : MonoBehaviour
{
    public GameObject chunkPrefab;
    public Material voxelMaterial;
    public int worldSizeInChunks = 4;

    [Header("Generation Seed")]
    public int seed = 1337;

    [Header("Texture Atlas Settings")]
    public int textureAtlasSizeInBlocks = 4;

    [Header("Block Definitions")]
    public BlockType[] blockTypes;

    private Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();

    private void Start()
    {
        // Only run on Start if world hasn't been generated in Editor beforehand
        if (chunks.Count == 0 && transform.childCount == 0)
        {
            GenerateWorld();
        }
    }

    public void GenerateWorld()
    {
        ClearWorld();

        // PASS 1: Generate Voxel Data
        for (int x = 0; x < worldSizeInChunks; x++)
        {
            for (int z = 0; z < worldSizeInChunks; z++)
            {
                Vector3Int chunkPos = new Vector3Int(x * VoxelData.ChunkWidth, 0, z * VoxelData.ChunkWidth);
                GameObject newChunkObject = Instantiate(chunkPrefab, chunkPos, Quaternion.identity, transform);

                newChunkObject.GetComponent<MeshRenderer>().material = voxelMaterial;

                Chunk newChunk = newChunkObject.GetComponent<Chunk>();
                newChunk.Initialize(this, chunkPos);

                if (!chunks.ContainsKey(chunkPos))
                {
                    chunks.Add(chunkPos, newChunk);
                }
            }
        }

        // PASS 2: Generate Meshes
        foreach (KeyValuePair<Vector3Int, Chunk> chunk in chunks)
        {
            chunk.Value.UpdateChunkMesh();
        }
    }

    public void ClearWorld()
    {
        chunks.Clear();

        // Collect all existing child chunk GameObjects
        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in transform)
        {
            children.Add(child.gameObject);
        }

        // Destroy child chunks using appropriate API for Edit vs Play mode
        foreach (GameObject child in children)
        {
            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }
    }

    public Chunk GetChunkFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / VoxelData.ChunkWidth) * VoxelData.ChunkWidth;
        int z = Mathf.FloorToInt(pos.z / VoxelData.ChunkWidth) * VoxelData.ChunkWidth;

        Vector3Int chunkPos = new Vector3Int(x, 0, z);

        if (chunks.ContainsKey(chunkPos))
        {
            return chunks[chunkPos];
        }
        return null;
    }

    public byte GetBlockID(Vector3 globalPosition)
    {
        Chunk targetChunk = GetChunkFromVector3(globalPosition);
        if (targetChunk != null)
        {
            Vector3Int localPos = new Vector3Int(
                Mathf.FloorToInt(globalPosition.x) - targetChunk.chunkPosition.x,
                Mathf.FloorToInt(globalPosition.y) - targetChunk.chunkPosition.y,
                Mathf.FloorToInt(globalPosition.z) - targetChunk.chunkPosition.z
            );

            return targetChunk.GetVoxelID(localPos);
        }
        return 0;
    }
}
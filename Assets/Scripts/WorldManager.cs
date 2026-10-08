using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public struct GenerationBlockIDs
{
    public byte air;
    public byte bedrock;
    public byte grass;
    public byte dirt;
    public byte stone;
}

public class WorldManager : MonoBehaviour
{
    [System.Serializable]
    public class BlockType
    {
        public string blockName;
        public bool isSolid = true;

        [Header("Texture Atlas Coordinates (X, Y)")]
        public int2 topTexture;
        public int2 bottomTexture;
        public int2 sideTexture;
    }

    public GameObject chunkPrefab;
    public Material voxelMaterial;
    public int worldSizeInChunks = 8;
    public int seed = 1337;

    [Header("Texture Atlas Settings")]
    public int textureAtlasSizeInBlocks = 4;

    [Header("Block Definitions")]
    public List<BlockType> blockTypes = new List<BlockType>();

    private NativeArray<int2> cachedUVMap;
    private Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();

    private void Start()
    {
        GenerateWorld();
    }

    public void GenerateWorld()
    {
        ClearWorld();

        for (int x = 0; x < worldSizeInChunks; x++)
        {
            for (int z = 0; z < worldSizeInChunks; z++)
            {
                Vector2Int pos = new Vector2Int(x, z);
                Vector3Int chunkPos = new Vector3Int(x * 16, 0, z * 16);

                GameObject chunkObj = Instantiate(chunkPrefab, chunkPos, Quaternion.identity, transform);
                Chunk chunk = chunkObj.GetComponent<Chunk>();

                chunk.Initialize(this, chunkPos);

                if (!Application.isPlaying)
                {
                    chunk.CompleteJobImmediately();
                }

                chunks[pos] = chunk;
            }
        }
    }

    public void ClearWorld()
    {
        chunks.Clear();
        DisposeNativeData();

        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in transform)
        {
            children.Add(child.gameObject);
        }

        foreach (GameObject child in children)
        {
            Chunk c = child.GetComponent<Chunk>();
            if (c != null) c.Cleanup();

            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
    }

    public Chunk GetChunkFromVector3(Vector3 worldPosition)
    {
        int chunkX = Mathf.FloorToInt(worldPosition.x / 16f);
        int chunkZ = Mathf.FloorToInt(worldPosition.z / 16f);
        Vector2Int chunkKey = new Vector2Int(chunkX, chunkZ);

        if (chunks.TryGetValue(chunkKey, out Chunk chunk))
        {
            return chunk;
        }

        return null;
    }

    public byte GetBlockID(string blockName)
    {
        for (int i = 0; i < blockTypes.Count; i++)
        {
            if (blockTypes[i].blockName.Equals(blockName, System.StringComparison.OrdinalIgnoreCase))
                return (byte)i;
        }
        Debug.LogWarning($"Block '{blockName}' not found! Defaulting to Air (0).");
        return 0;
    }

    public GenerationBlockIDs GetGenerationBlockIDs()
    {
        return new GenerationBlockIDs
        {
            air = GetBlockID("Air"),
            bedrock = GetBlockID("Bedrock"),
            grass = GetBlockID("Grass"),
            dirt = GetBlockID("Dirt"),
            stone = GetBlockID("Stone")
        };
    }

    public NativeArray<int2> GetNativeUVMap()
    {
        if (cachedUVMap.IsCreated) return cachedUVMap;

        cachedUVMap = new NativeArray<int2>(math.max(1, blockTypes.Count) * 6, Allocator.Persistent);

        for (int i = 0; i < blockTypes.Count; i++)
        {
            BlockType block = blockTypes[i];

            // Face order: 0=Front, 1=Back, 2=Top, 3=Bottom, 4=Left, 5=Right
            cachedUVMap[(i * 6) + 0] = block.sideTexture;
            cachedUVMap[(i * 6) + 1] = block.sideTexture;
            cachedUVMap[(i * 6) + 2] = block.topTexture;
            cachedUVMap[(i * 6) + 3] = block.bottomTexture;
            cachedUVMap[(i * 6) + 4] = block.sideTexture;
            cachedUVMap[(i * 6) + 5] = block.sideTexture;
        }

        return cachedUVMap;
    }

    private void DisposeNativeData()
    {
        if (cachedUVMap.IsCreated)
        {
            cachedUVMap.Dispose();
        }
    }

    private void OnDisable()
    {
        DisposeNativeData();
    }

    private void OnDestroy()
    {
        DisposeNativeData();
    }
}
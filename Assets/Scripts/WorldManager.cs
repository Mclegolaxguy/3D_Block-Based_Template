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
        public bool isTransparent = false;
        public Material blockMaterial;

        [Header("Texture Configuration")]
        public bool seperateFaces;
        public int textureAtlasSizeInBlocks = 4;

        [Header("Atlas Texture Coords (int2)")]
        public int2 singleTexture;
        public int2 topTexture;
        public int2 bottomTexture;
        public int2 frontTexture;
        public int2 backTexture;
        public int2 leftTexture;
        public int2 rightTexture;
    }

    public GameObject chunkPrefab;
    public int worldSizeInChunks = 1;
    public int seed = 0;

    [Header("Block Definitions")]
    public List<BlockType> blockTypes = new List<BlockType>();

    private NativeArray<float4> cachedUVMap;
    private NativeArray<int> cachedMaterialIndices;
    private List<Material> cachedUniqueMaterials;
    private Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();
    private Dictionary<string, byte> blockNameToID = new Dictionary<string, byte>(System.StringComparer.OrdinalIgnoreCase);

    private void Awake()
    {
        BuildBlockDictionary();
    }

    private void Start()
    {
        GenerateWorld();
    }

    public void BuildBlockDictionary()
    {
        blockNameToID.Clear();
        for (int i = 0; i < blockTypes.Count; i++)
        {
            if (!string.IsNullOrEmpty(blockTypes[i].blockName))
            {
                blockNameToID[blockTypes[i].blockName] = (byte)i;
            }
        }
    }

    public void GenerateWorld()
    {
        BuildBlockDictionary();
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

        chunks.TryGetValue(chunkKey, out Chunk chunk);
        return chunk;
    }

    public byte GetBlockID(string blockName)
    {
        if (blockNameToID.TryGetValue(blockName, out byte id))
        {
            return id;
        }

        for (int i = 0; i < blockTypes.Count; i++)
        {
            if (blockTypes[i].blockName.Equals(blockName, System.StringComparison.OrdinalIgnoreCase))
            {
                blockNameToID[blockName] = (byte)i;
                return (byte)i;
            }
        }

        Debug.LogWarning($"Block '{blockName}' not found by name lookup! Defaulting to Air (0).");
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

    public NativeArray<float4> GetNativeUVMap()
    {
        if (cachedUVMap.IsCreated) return cachedUVMap;

        int count = math.max(1, blockTypes.Count);
        cachedUVMap = new NativeArray<float4>(count * 6, Allocator.Persistent);
        float pad = 0.001f;

        for (int i = 0; i < blockTypes.Count; i++)
        {
            BlockType block = blockTypes[i];
            int baseIndex = i * 6;

            float atlasSize = Mathf.Max(1, block.textureAtlasSizeInBlocks);
            float size = 1f / atlasSize;

            int2 f = block.seperateFaces ? block.frontTexture : block.singleTexture;
            int2 b = block.seperateFaces ? block.backTexture : block.singleTexture;
            int2 t = block.seperateFaces ? block.topTexture : block.singleTexture;
            int2 bt = block.seperateFaces ? block.bottomTexture : block.singleTexture;
            int2 l = block.seperateFaces ? block.leftTexture : block.singleTexture;
            int2 r = block.seperateFaces ? block.rightTexture : block.singleTexture;

            cachedUVMap[baseIndex + 0] = GetAtlasUVRect(f, size, pad);
            cachedUVMap[baseIndex + 1] = GetAtlasUVRect(b, size, pad);
            cachedUVMap[baseIndex + 2] = GetAtlasUVRect(t, size, pad);
            cachedUVMap[baseIndex + 3] = GetAtlasUVRect(bt, size, pad);
            cachedUVMap[baseIndex + 4] = GetAtlasUVRect(l, size, pad);
            cachedUVMap[baseIndex + 5] = GetAtlasUVRect(r, size, pad);
        }

        return cachedUVMap;
    }

    public List<Material> GetUniqueMaterials(out NativeArray<int> materialIndicesOut)
    {
        if (cachedUniqueMaterials == null || !cachedMaterialIndices.IsCreated)
        {
            cachedUniqueMaterials = new List<Material>();
            Dictionary<Material, int> matMap = new Dictionary<Material, int>();
            Material defaultMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));

            int count = math.max(1, blockTypes.Count);
            cachedMaterialIndices = new NativeArray<int>(count, Allocator.Persistent);

            for (int i = 0; i < blockTypes.Count; i++)
            {
                Material mat = blockTypes[i].blockMaterial != null ? blockTypes[i].blockMaterial : defaultMat;
                if (!matMap.TryGetValue(mat, out int matIndex))
                {
                    matIndex = cachedUniqueMaterials.Count;
                    matMap[mat] = matIndex;
                    cachedUniqueMaterials.Add(mat);
                }
                cachedMaterialIndices[i] = matIndex;
            }

            if (cachedUniqueMaterials.Count == 0)
            {
                cachedUniqueMaterials.Add(defaultMat);
                cachedMaterialIndices[0] = 0;
            }
        }

        materialIndicesOut = cachedMaterialIndices;
        return cachedUniqueMaterials;
    }

    private float4 GetAtlasUVRect(int2 texPos, float size, float pad)
    {
        float minU = texPos.x * size;
        float minV = texPos.y * size;
        float maxU = minU + size;
        float maxV = minV + size;
        return new float4(minU + pad, minV + pad, maxU - pad, maxV - pad);
    }

    private void DisposeNativeData()
    {
        if (cachedUVMap.IsCreated) cachedUVMap.Dispose();
        if (cachedMaterialIndices.IsCreated) cachedMaterialIndices.Dispose();
        cachedUniqueMaterials = null;
    }

    private void OnDisable() => DisposeNativeData();
    private void OnDestroy() => DisposeNativeData();
}
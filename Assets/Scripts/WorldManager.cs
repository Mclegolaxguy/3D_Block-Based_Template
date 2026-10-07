using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public class WorldManager : MonoBehaviour
{
    public GameObject chunkPrefab;
    public Material voxelMaterial;
    public int worldSizeInChunks = 4;
    public int seed = 1337;

    [Header("Texture Atlas Settings")]
    public int textureAtlasSizeInBlocks = 4;

    [Header("Block Definitions")]
    public BlockType[] blockTypes;

    private Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();
    private NativeArray<int2> sharedUVMap;

    private void Start()
    {
        if (chunks.Count == 0 && transform.childCount == 0)
        {
            GenerateWorld();
        }
    }

    public NativeArray<int2> GetNativeUVMap()
    {
        return sharedUVMap;
    }

    public void GenerateWorld()
    {
        ClearWorld();

        sharedUVMap = new NativeArray<int2>(blockTypes.Length * 6, Allocator.Persistent);

        for (int i = 0; i < blockTypes.Length; i++)
        {
            sharedUVMap[(i * 6) + 0] = new int2(blockTypes[i].backFaceTexture.x, blockTypes[i].backFaceTexture.y);
            sharedUVMap[(i * 6) + 1] = new int2(blockTypes[i].frontFaceTexture.x, blockTypes[i].frontFaceTexture.y);
            sharedUVMap[(i * 6) + 2] = new int2(blockTypes[i].topFaceTexture.x, blockTypes[i].topFaceTexture.y);
            sharedUVMap[(i * 6) + 3] = new int2(blockTypes[i].bottomFaceTexture.x, blockTypes[i].bottomFaceTexture.y);
            sharedUVMap[(i * 6) + 4] = new int2(blockTypes[i].leftFaceTexture.x, blockTypes[i].leftFaceTexture.y);
            sharedUVMap[(i * 6) + 5] = new int2(blockTypes[i].rightFaceTexture.x, blockTypes[i].rightFaceTexture.y);
        }

        for (int x = 0; x < worldSizeInChunks; x++)
        {
            for (int z = 0; z < worldSizeInChunks; z++)
            {
                Vector3Int chunkPos = new Vector3Int(x * 16, 0, z * 16);
                GameObject newChunkObject = Instantiate(chunkPrefab, chunkPos, Quaternion.identity, transform);

                newChunkObject.GetComponent<MeshRenderer>().material = voxelMaterial;

                Chunk newChunk = newChunkObject.GetComponent<Chunk>();

                if (!chunks.ContainsKey(chunkPos))
                {
                    chunks.Add(chunkPos, newChunk);
                }

                newChunk.Initialize(this, chunkPos);
            }
        }
    }

    public void ClearWorld()
    {
        chunks.Clear();

        if (sharedUVMap.IsCreated)
        {
            sharedUVMap.Dispose();
        }

        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in transform) children.Add(child.gameObject);

        foreach (GameObject child in children)
        {
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
    }

    public Chunk GetChunkFromVector3(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x / 16f) * 16;
        int z = Mathf.FloorToInt(pos.z / 16f) * 16;

        Vector3Int chunkPos = new Vector3Int(x, 0, z);

        if (chunks.TryGetValue(chunkPos, out Chunk chunk))
        {
            return chunk;
        }
        return null;
    }
}
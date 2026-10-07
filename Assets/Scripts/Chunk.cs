using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;
    private Mesh chunkMesh;

    public WorldManager world;
    public Vector3Int chunkPosition;

    private NativeArray<byte> nativeVoxelMap;

    public void Initialize(WorldManager worldManager, Vector3Int position)
    {
        world = worldManager;
        chunkPosition = position;
        transform.position = chunkPosition;

        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
        chunkMesh = new Mesh();
        chunkMesh.MarkDynamic();
        meshFilter.mesh = chunkMesh;

        nativeVoxelMap = new NativeArray<byte>(16 * 64 * 16, Allocator.Persistent);

        GenerateUsingJobs();
    }

    public void GenerateUsingJobs()
    {
        int maxBlocks = 16384;
        int maxVerts = maxBlocks * 24;
        int maxTris = maxBlocks * 36;

        NativeArray<float3> verts = new NativeArray<float3>(maxVerts, Allocator.TempJob);
        NativeArray<int> tris = new NativeArray<int>(maxTris, Allocator.TempJob);
        NativeArray<float2> uvs = new NativeArray<float2>(maxVerts, Allocator.TempJob);
        NativeArray<int> counter = new NativeArray<int>(1, Allocator.TempJob);

        ChunkBuilderJob buildJob = new ChunkBuilderJob
        {
            seed = world.seed,
            chunkGlobalPos = new int2(chunkPosition.x, chunkPosition.z),
            atlasSize = world.textureAtlasSizeInBlocks,
            blockFaceUVs = world.GetNativeUVMap(),
            voxelMap = nativeVoxelMap,
            vertices = verts,
            triangles = tris,
            uvs = uvs,
            counter = counter
        };

        JobHandle handle = buildJob.Schedule();
        handle.Complete();

        int actualVertexCount = counter[0];
        int actualTriangleCount = (actualVertexCount / 4) * 6;

        chunkMesh.Clear();

        if (actualVertexCount > 0)
        {
            chunkMesh.SetVertices(verts, 0, actualVertexCount);
            chunkMesh.SetIndices(tris, 0, actualTriangleCount, MeshTopology.Triangles, 0, false);
            chunkMesh.SetUVs(0, uvs, 0, actualVertexCount);
            chunkMesh.RecalculateNormals();
        }

        meshCollider.sharedMesh = chunkMesh;

        verts.Dispose();
        tris.Dispose();
        uvs.Dispose();
        counter.Dispose();
    }

    public void EditVoxel(Vector3Int localPos, byte newBlockID)
    {
        if (localPos.x < 0 || localPos.x >= 16 ||
            localPos.y < 0 || localPos.y >= 64 ||
            localPos.z < 0 || localPos.z >= 16) return;

        int index = localPos.x + (localPos.y * 16) + (localPos.z * 16 * 64);
        nativeVoxelMap[index] = newBlockID;

        GenerateUsingJobs();
    }

    private void OnDestroy()
    {
        if (nativeVoxelMap.IsCreated)
        {
            nativeVoxelMap.Dispose();
        }
    }
}
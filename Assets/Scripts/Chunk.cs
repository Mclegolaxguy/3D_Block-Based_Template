using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    public Vector3Int chunkPosition;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private Mesh mesh;
    private WorldManager worldManager;

    private NativeArray<byte> voxelMap;
    private NativeArray<float3> vertices;
    private NativeArray<int> triangles;
    private NativeArray<float2> uvs;
    private NativeArray<int> counter;

    private JobHandle jobHandle;
    private ChunkBuilderJob job;
    private bool isJobScheduled;

    public void Initialize(WorldManager worldManager, Vector3Int chunkPos)
    {
        this.worldManager = worldManager;
        this.chunkPosition = chunkPos;
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();

        // Assign the Voxel Material from WorldManager
        if (worldManager.voxelMaterial != null)
        {
            meshRenderer.sharedMaterial = worldManager.voxelMaterial;
        }

        mesh = new Mesh { name = $"Chunk_{chunkPos.x}_{chunkPos.z}" };
        meshFilter.mesh = mesh;

        voxelMap = new NativeArray<byte>(16 * 64 * 16, Allocator.Persistent);
        vertices = new NativeArray<float3>(196608, Allocator.Persistent);
        triangles = new NativeArray<int>(294912, Allocator.Persistent);
        uvs = new NativeArray<float2>(196608, Allocator.Persistent);
        counter = new NativeArray<int>(1, Allocator.Persistent);

        ScheduleMeshBuild(true);
    }

    public void EditVoxel(Vector3Int localPos, byte newBlockID)
    {
        EditVoxel(localPos.x, localPos.y, localPos.z, newBlockID);
    }

    public void EditVoxel(int x, int y, int z, byte newBlockID)
    {
        if (x < 0 || x >= 16 || y < 0 || y >= 64 || z < 0 || z >= 16) return;

        if (isJobScheduled)
        {
            jobHandle.Complete();
            isJobScheduled = false;
        }

        int flatIndex = x + (y * 16) + (z * 16 * 64);
        if (voxelMap.IsCreated)
        {
            voxelMap[flatIndex] = newBlockID;
            ScheduleMeshBuild(false);
        }
    }

    private void ScheduleMeshBuild(bool generateVoxels)
    {
        if (isJobScheduled)
        {
            jobHandle.Complete();
            isJobScheduled = false;
        }

        job = new ChunkBuilderJob
        {
            seed = worldManager.seed,
            chunkGlobalPos = new int2(chunkPosition.x, chunkPosition.z),
            atlasSize = worldManager.textureAtlasSizeInBlocks,
            generateVoxels = generateVoxels,
            blockIDs = worldManager.GetGenerationBlockIDs(),
            blockFaceUVs = worldManager.GetNativeUVMap(),
            voxelMap = voxelMap,
            vertices = vertices,
            triangles = triangles,
            uvs = uvs,
            counter = counter
        };

        jobHandle = job.Schedule();
        isJobScheduled = true;
    }

    public void CompleteJobImmediately()
    {
        if (isJobScheduled)
        {
            jobHandle.Complete();
            isJobScheduled = false;
            BuildMesh();
        }
    }

    private void Update()
    {
        if (isJobScheduled && jobHandle.IsCompleted)
        {
            jobHandle.Complete();
            isJobScheduled = false;
            BuildMesh();
        }
    }

    private void BuildMesh()
    {
        int vertexCount = counter[0];
        int triangleCount = (vertexCount * 6) / 4;

        mesh.Clear();
        if (vertexCount > 0)
        {
            mesh.SetVertices(vertices.Reinterpret<Vector3>(), 0, vertexCount);
            mesh.SetUVs(0, uvs.Reinterpret<Vector2>(), 0, vertexCount);
            mesh.SetTriangles(triangles.Slice(0, triangleCount).ToArray(), 0);

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            meshCollider.sharedMesh = mesh;
        }
    }

    public void Cleanup()
    {
        if (isJobScheduled)
        {
            jobHandle.Complete();
            isJobScheduled = false;
        }

        if (voxelMap.IsCreated) voxelMap.Dispose();
        if (vertices.IsCreated) vertices.Dispose();
        if (triangles.IsCreated) triangles.Dispose();
        if (uvs.IsCreated) uvs.Dispose();
        if (counter.IsCreated) counter.Dispose();
    }

    private void OnDestroy()
    {
        Cleanup();
    }
}
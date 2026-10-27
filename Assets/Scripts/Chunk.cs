using System.Collections.Generic;
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
    private NativeArray<float4> colors;
    private NativeArray<int> counter;
    private NativeArray<int> triangleSubmeshIndices;

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

        List<Material> uniqueMaterials = worldManager.GetUniqueMaterials(out NativeArray<int> materialIndices);
        meshRenderer.sharedMaterials = uniqueMaterials.ToArray();

        mesh = new Mesh { name = $"Chunk_{chunkPos.x}_{chunkPos.z}" };
        meshFilter.mesh = mesh;

        voxelMap = new NativeArray<byte>(16 * 64 * 16, Allocator.Persistent);
        vertices = new NativeArray<float3>(196608, Allocator.Persistent);
        triangles = new NativeArray<int>(294912, Allocator.Persistent);
        uvs = new NativeArray<float2>(196608, Allocator.Persistent);
        colors = new NativeArray<float4>(196608, Allocator.Persistent);
        counter = new NativeArray<int>(1, Allocator.Persistent);
        triangleSubmeshIndices = new NativeArray<int>(49152, Allocator.Persistent);

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

        worldManager.GetUniqueMaterials(out NativeArray<int> materialIndices);

        job = new ChunkBuilderJob
        {
            seed = worldManager.seed,
            chunkGlobalPos = new int2(chunkPosition.x, chunkPosition.z),
            generateVoxels = generateVoxels,
            blockIDs = worldManager.GetGenerationBlockIDs(),
            blockFaceUVs = worldManager.GetNativeUVMap(),
            blockFaceColors = worldManager.GetNativeColorMap(),
            blockMaterialIndices = materialIndices,
            voxelMap = voxelMap,
            vertices = vertices,
            triangles = triangles,
            uvs = uvs,
            colors = colors,
            counter = counter,
            triangleSubmeshIndices = triangleSubmeshIndices
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
        if (mesh == null) return;

        int vertexCount = counter[0];
        int totalFaces = vertexCount / 4;

        mesh.Clear();
        if (vertexCount > 0)
        {
            List<Material> uniqueMaterials = worldManager.GetUniqueMaterials(out _);
            int submeshCount = uniqueMaterials.Count;

            List<int>[] submeshTriangles = new List<int>[submeshCount];
            for (int i = 0; i < submeshCount; i++)
            {
                submeshTriangles[i] = new List<int>();
            }

            for (int f = 0; f < totalFaces; f++)
            {
                int submeshIdx = triangleSubmeshIndices[f];
                if (submeshIdx < 0 || submeshIdx >= submeshCount) submeshIdx = 0;

                int triStartIndex = f * 6;
                for (int i = 0; i < 6; i++)
                {
                    submeshTriangles[submeshIdx].Add(triangles[triStartIndex + i]);
                }
            }

            mesh.SetVertices(vertices.Reinterpret<Vector3>(), 0, vertexCount);
            mesh.SetUVs(0, uvs.Reinterpret<Vector2>(), 0, vertexCount);
            mesh.SetColors(colors.Reinterpret<Color>(), 0, vertexCount);

            mesh.subMeshCount = submeshCount;
            for (int i = 0; i < submeshCount; i++)
            {
                mesh.SetTriangles(submeshTriangles[i], i);
            }

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            if (meshCollider != null) meshCollider.sharedMesh = mesh;
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
        if (colors.IsCreated) colors.Dispose();
        if (counter.IsCreated) counter.Dispose();
        if (triangleSubmeshIndices.IsCreated) triangleSubmeshIndices.Dispose();
    }

    private void OnDestroy()
    {
        Cleanup();
    }
}
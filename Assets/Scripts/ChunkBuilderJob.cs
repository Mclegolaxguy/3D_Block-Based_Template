using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct ChunkBuilderJob : IJob
{
    [ReadOnly] public int seed;
    [ReadOnly] public int2 chunkGlobalPos;
    [ReadOnly] public int atlasSize;
    [ReadOnly] public bool generateVoxels;
    [ReadOnly] public GenerationBlockIDs blockIDs;
    [ReadOnly] public NativeArray<int2> blockFaceUVs;

    public NativeArray<byte> voxelMap;
    public NativeArray<float3> vertices;
    public NativeArray<int> triangles;
    public NativeArray<float2> uvs;
    public NativeArray<int> counter;

    public void Execute()
    {
        counter[0] = 0;

        // 1. GENERATE VOXEL DATA
        if (generateVoxels)
        {
            for (int x = 0; x < 16; x++)
            {
                for (int z = 0; z < 16; z++)
                {
                    int globalX = x + chunkGlobalPos.x;
                    int globalZ = z + chunkGlobalPos.y;

                    for (int y = 0; y < 64; y++)
                    {
                        byte blockID = GenerateBlockAtGlobal(globalX, y, globalZ);
                        voxelMap[GetFlatIndex(x, y, z)] = blockID;
                    }
                }
            }
        }

        // 2. BUILD MESH DATA
        for (int x = 0; x < 16; x++)
        {
            for (int y = 0; y < 64; y++)
            {
                for (int z = 0; z < 16; z++)
                {
                    byte blockID = voxelMap[GetFlatIndex(x, y, z)];
                    if (blockID == blockIDs.air) continue;

                    for (int face = 0; face < 6; face++)
                    {
                        if (!IsNeighborSolid(x, y, z, face))
                        {
                            DrawFace(x, y, z, blockID, face);
                        }
                    }
                }
            }
        }
    }

    private byte GenerateBlockAtGlobal(int globalX, int globalY, int globalZ)
    {
        if (globalY <= 0) return blockIDs.bedrock;

        float mMask = noise.cnoise(new float2(globalX * 0.006f + seed + 500, globalZ * 0.006f + seed + 500));
        mMask = math.max(0f, mMask);
        mMask = math.pow(mMask, 1.5f);

        float hills = (noise.cnoise(new float2(globalX * 0.015f + seed, globalZ * 0.015f + seed)) + 1f) * 0.5f;
        float mountainDetail = (noise.cnoise(new float2(globalX * 0.03f + seed + 200, globalZ * 0.03f + seed + 200)) + 1) * 0.5f;

        int surfaceHeight = 20 + (int)(hills * 8f) + (int)(mountainDetail * 28f * mMask);
        surfaceHeight = math.clamp(surfaceHeight, 5, 60);

        if (globalY > surfaceHeight) return blockIDs.air;

        if (globalY > 1 && globalY <= surfaceHeight)
        {
            float cave1 = noise.cnoise(new float3(globalX * 0.03f + seed, globalY * 0.03f + seed, globalZ * 0.03f + seed));
            float cave2 = noise.cnoise(new float3(globalX * 0.06f + seed + 100, globalY * 0.06f + seed + 100, globalZ * 0.06f + seed + 100));

            float caveDensity = math.abs(cave1) + math.abs(cave2) * 0.5f;
            float heightRatio = (float)globalY / surfaceHeight;
            float caveThreshold = math.lerp(0.18f, 0.12f, heightRatio);

            if (caveDensity < caveThreshold)
            {
                return blockIDs.air;
            }
        }

        bool isMountainPeak = surfaceHeight > 36;
        if (globalY == surfaceHeight)
        {
            return isMountainPeak ? blockIDs.stone : blockIDs.grass;
        }
        if (globalY >= surfaceHeight - 3)
        {
            return isMountainPeak ? blockIDs.stone : blockIDs.dirt;
        }
        return blockIDs.stone;
    }

    private bool IsNeighborSolid(int localX, int localY, int localZ, int face)
    {
        int3 offset = GetFaceNormal(face);
        int nx = localX + offset.x;
        int ny = localY + offset.y;
        int nz = localZ + offset.z;

        if (nx < 0 || nx > 15 || nz < 0 || nz > 15)
        {
            int globalX = nx + chunkGlobalPos.x;
            int globalZ = nz + chunkGlobalPos.y;
            return GenerateBlockAtGlobal(globalX, ny, globalZ) != blockIDs.air;
        }

        if (ny < 0 || ny > 63) return false;

        return voxelMap[GetFlatIndex(nx, ny, nz)] != blockIDs.air;
    }

    private void DrawFace(int x, int y, int z, byte blockID, int face)
    {
        int vIndex = counter[0];
        int4 triData = GetTriangleIndices(face);

        vertices[vIndex] = GetCornerVertex(triData.x) + new float3(x, y, z);
        vertices[vIndex + 1] = GetCornerVertex(triData.y) + new float3(x, y, z);
        vertices[vIndex + 2] = GetCornerVertex(triData.z) + new float3(x, y, z);
        vertices[vIndex + 3] = GetCornerVertex(triData.w) + new float3(x, y, z);

        int uvLookupIndex = (blockID * 6) + face;
        int2 texPos = blockFaceUVs[uvLookupIndex];
        float normalizedSize = 1f / atlasSize;
        float uvX = texPos.x * normalizedSize;
        float uvY = texPos.y * normalizedSize;

        float pad = 0.001f;

        uvs[vIndex] = new float2(uvX + pad, uvY + pad);
        uvs[vIndex + 1] = new float2(uvX + pad, uvY + normalizedSize - pad);
        uvs[vIndex + 2] = new float2(uvX + normalizedSize - pad, uvY + pad);
        uvs[vIndex + 3] = new float2(uvX + normalizedSize - pad, uvY + normalizedSize - pad);

        triangles[(vIndex * 3 / 2)] = vIndex;
        triangles[(vIndex * 3 / 2) + 1] = vIndex + 1;
        triangles[(vIndex * 3 / 2) + 2] = vIndex + 2;
        triangles[(vIndex * 3 / 2) + 3] = vIndex + 2;
        triangles[(vIndex * 3 / 2) + 4] = vIndex + 1;
        triangles[(vIndex * 3 / 2) + 5] = vIndex + 3;

        counter[0] += 4;
    }

    private int GetFlatIndex(int x, int y, int z) => x + (y * 16) + (z * 16 * 64);

    private int3 GetFaceNormal(int face)
    {
        if (face == 0) return new int3(0, 0, 1);
        if (face == 1) return new int3(0, 0, -1);
        if (face == 2) return new int3(0, 1, 0);
        if (face == 3) return new int3(0, -1, 0);
        if (face == 4) return new int3(-1, 0, 0);
        return new int3(1, 0, 0);
    }

    private float3 GetCornerVertex(int index)
    {
        if (index == 0) return new float3(0, 0, 0);
        if (index == 1) return new float3(1, 0, 0);
        if (index == 2) return new float3(1, 1, 0);
        if (index == 3) return new float3(0, 1, 0);
        if (index == 4) return new float3(0, 0, 1);
        if (index == 5) return new float3(1, 0, 1);
        if (index == 6) return new float3(1, 1, 1);
        return new float3(0, 1, 1);
    }

    private int4 GetTriangleIndices(int face)
    {
        // FIXED: Reversed winding order so Front and Back normals point outward.
        if (face == 0) return new int4(5, 6, 4, 7); // Front (+Z)
        if (face == 1) return new int4(0, 3, 1, 2); // Back (-Z)
        if (face == 2) return new int4(3, 7, 2, 6); // Top (+Y)
        if (face == 3) return new int4(4, 0, 5, 1); // Bottom (-Y)
        if (face == 4) return new int4(4, 7, 0, 3); // Left (-X)
        return new int4(1, 2, 5, 6);               // Right (+X)
    }
}
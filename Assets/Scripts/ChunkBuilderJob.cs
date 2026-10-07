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

    // UV map lookup (passed from WorldManager)
    [ReadOnly] public NativeArray<int2> blockFaceUVs;

    // Persistent voxel data for this specific chunk
    public NativeArray<byte> voxelMap;

    // Mesh Data arrays allocated to max possible size
    public NativeArray<float3> vertices;
    public NativeArray<int> triangles;
    public NativeArray<float2> uvs;

    // Counter to track how many vertices we actually created
    public NativeArray<int> counter;

    public void Execute()
    {
        counter[0] = 0;

        // 1. GENERATE VOXEL DATA
        for (int x = 0; x < 16; x++)
        {
            for (int z = 0; z < 16; z++)
            {
                int height = CalculateTerrainHeight(x + chunkGlobalPos.x, z + chunkGlobalPos.y);

                for (int y = 0; y < 64; y++)
                {
                    int index = GetFlatIndex(x, y, z);

                    if (y > height) voxelMap[index] = 0;      // Air
                    else if (y == height) voxelMap[index] = 2;// Grass
                    else voxelMap[index] = 1;                 // Dirt
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
                    if (blockID == 0) continue;

                    // Check all 6 directions (0=Back, 1=Front, 2=Top, 3=Bottom, 4=Left, 5=Right)
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

    private int CalculateTerrainHeight(int globalX, int globalZ)
    {
        // Unity.Mathematics noise function via noise.cnoise
        float noiseVal = noise.cnoise(new float2(globalX * 0.05f + seed, globalZ * 0.05f + seed));
        return (int)math.floor(noiseVal * 12f) + 10;
    }

    private bool IsNeighborSolid(int localX, int localY, int localZ, int face)
    {
        int3 offset = GetFaceNormal(face);
        int nx = localX + offset.x;
        int ny = localY + offset.y;
        int nz = localZ + offset.z;

        // If checking outside chunk bounds, use math instead of looking up data
        if (nx < 0 || nx > 15 || nz < 0 || nz > 15)
        {
            int neighborHeight = CalculateTerrainHeight(nx + chunkGlobalPos.x, nz + chunkGlobalPos.y);
            return ny <= neighborHeight;
        }

        // Vertical bounds
        if (ny < 0 || ny > 63) return false;

        // Inside bounds, read from our native array
        return voxelMap[GetFlatIndex(nx, ny, nz)] != 0;
    }

    private void DrawFace(int x, int y, int z, byte blockID, int face)
    {
        int vIndex = counter[0];

        // Get the 4 corners of this face
        int4 triData = GetTriangleIndices(face);

        vertices[vIndex] = GetCornerVertex(triData.x) + new float3(x, y, z);
        vertices[vIndex + 1] = GetCornerVertex(triData.y) + new float3(x, y, z);
        vertices[vIndex + 2] = GetCornerVertex(triData.z) + new float3(x, y, z);
        vertices[vIndex + 3] = GetCornerVertex(triData.w) + new float3(x, y, z);

        // Calculate UVs based on block ID and Face
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

        // Triangles are drawn in two polygons
        triangles[vIndex * 3 / 2] = vIndex;
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
        if (face == 0) return new int3(0, 0, -1);
        if (face == 1) return new int3(0, 0, 1);
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
        if (face == 0) return new int4(0, 3, 1, 2);
        if (face == 1) return new int4(5, 6, 4, 7);
        if (face == 2) return new int4(3, 7, 2, 6);
        if (face == 3) return new int4(1, 5, 0, 4);
        if (face == 4) return new int4(4, 7, 0, 3);
        return new int4(1, 2, 5, 6);
    }
}
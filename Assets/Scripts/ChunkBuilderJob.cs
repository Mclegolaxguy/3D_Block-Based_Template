using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct ChunkBuilderJob : IJob
{
    [ReadOnly] public int seed;
    [ReadOnly] public int2 chunkGlobalPos; // x = world X, y = world Z
    [ReadOnly] public int atlasSize;

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
        for (int x = 0; x < 16; x++)
        {
            for (int z = 0; z < 16; z++)
            {
                int globalX = x + chunkGlobalPos.x;
                int globalZ = z + chunkGlobalPos.y;

                for (int y = 0; y < 64; y++)
                {
                    // Generate the specific block based on world rules
                    byte blockID = GenerateBlockAtGlobal(globalX, y, globalZ);
                    voxelMap[GetFlatIndex(x, y, z)] = blockID;
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

    // This method contains the "Minecraft" ruleset for terrain and caves
    private byte GenerateBlockAtGlobal(int globalX, int globalY, int globalZ)
    {
        if (globalY <= 0) return 4; // Bedrock is always at the absolute bottom

        // SURFACE GENERATION (2D Noise Octaves)
        // Elevation provides large rolling hills, Roughness provides small local bumps
        float elevation = noise.cnoise(new float2(globalX * 0.01f + seed, globalZ * 0.01f + seed));
        float roughness = noise.cnoise(new float2(globalX * 0.04f + seed, globalZ * 0.04f + seed));

        // cnoise returns -1 to 1. We normalize it to 0 to 1 for easier height calculation.
        elevation = (elevation + 1f) / 2f;
        roughness = (roughness + 1f) / 2f;

        // Calculate surface height (Base height of 25 + up to 20 for hills + up to 5 for bumps)
        int surfaceHeight = 25 + (int)(elevation * 20f) + (int)(roughness * 5f);

        if (globalY > surfaceHeight) return 0; // Air above the surface

        // CAVE GENERATION (3D Noise)
        // We only generate caves deep underground to avoid floating grass/dirt
        if (globalY > 1 && globalY < surfaceHeight - 3)
        {
            float caveNoise = noise.cnoise(new float3(globalX * 0.05f + seed, globalY * 0.05f + seed, globalZ * 0.05f + seed));

            // If the 3D noise crosses a threshold, carve out air instead of stone
            if (caveNoise > 0.35f) return 0;
        }

        // BIOME / DEPTH LAYERING
        if (globalY == surfaceHeight) return 2; // Top layer is Grass
        if (globalY >= surfaceHeight - 3) return 1; // 3 layers of Dirt below grass

        return 3; // Everything else is Stone
    }

    private bool IsNeighborSolid(int localX, int localY, int localZ, int face)
    {
        int3 offset = GetFaceNormal(face);
        int nx = localX + offset.x;
        int ny = localY + offset.y;
        int nz = localZ + offset.z;

        // If a face borders an unloaded chunk, we use our mathematical rule to "predict" 
        // what is there so chunks mesh perfectly without waiting for their neighbors.
        if (nx < 0 || nx > 15 || nz < 0 || nz > 15)
        {
            int globalX = nx + chunkGlobalPos.x;
            int globalZ = nz + chunkGlobalPos.y;
            return GenerateBlockAtGlobal(globalX, ny, globalZ) != 0;
        }

        // Vertical bounds
        if (ny < 0 || ny > 63) return false;

        // Inside bounds, read from our native array
        return voxelMap[GetFlatIndex(nx, ny, nz)] != 0;
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
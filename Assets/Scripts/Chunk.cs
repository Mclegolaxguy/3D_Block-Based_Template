using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;

    private byte[,,] voxelMap;
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>();

    private int vertexIndex = 0;
    public WorldManager world;
    public Vector3Int chunkPosition;

    public void Initialize(WorldManager worldManager, Vector3Int position)
    {
        world = worldManager;
        chunkPosition = position;
        transform.position = chunkPosition;

        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();

        PopulateVoxelMap();
    }

    private void PopulateVoxelMap()
    {
        voxelMap = new byte[VoxelData.ChunkWidth, VoxelData.ChunkHeight, VoxelData.ChunkWidth];

        for (int x = 0; x < VoxelData.ChunkWidth; x++)
        {
            for (int z = 0; z < VoxelData.ChunkWidth; z++)
            {
                // Use global position combined with world seed for smooth Perlin terrain across chunks
                float globalX = (x + chunkPosition.x) * 0.05f + world.seed;
                float globalZ = (z + chunkPosition.z) * 0.05f + world.seed;

                // Calculates terrain height based on seed
                int terrainHeight = Mathf.FloorToInt(Mathf.PerlinNoise(globalX, globalZ) * 12f) + 10;

                for (int y = 0; y < VoxelData.ChunkHeight; y++)
                {
                    if (y > terrainHeight)
                        voxelMap[x, y, z] = 0; // Air
                    else if (y == terrainHeight)
                        voxelMap[x, y, z] = 2; // Grass
                    else
                        voxelMap[x, y, z] = 1; // Dirt
                }
            }
        }
    }

    public void UpdateChunkMesh()
    {
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
        vertexIndex = 0;

        for (int x = 0; x < VoxelData.ChunkWidth; x++)
        {
            for (int y = 0; y < VoxelData.ChunkHeight; y++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    byte blockID = voxelMap[x, y, z];
                    if (blockID != 0)
                    {
                        AddVoxelDataToChunk(new Vector3Int(x, y, z), blockID);
                    }
                }
            }
        }

        CreateMesh();
    }

    private void AddVoxelDataToChunk(Vector3Int pos, byte blockID)
    {
        for (int p = 0; p < 6; p++)
        {
            if (!CheckVoxel(pos + Vector3Int.RoundToInt(VoxelData.faceChecks[p])))
            {
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 0]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 1]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 2]]);
                vertices.Add(pos + VoxelData.voxelVerts[VoxelData.voxelTris[p, 3]]);

                AddTextureUVs(blockID, p);

                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 3);

                vertexIndex += 4;
            }
        }
    }

    private void AddTextureUVs(byte blockID, int faceIndex)
    {
        Vector2Int texturePos = world.blockTypes[blockID].GetTextureID(faceIndex);
        float normalizedBlockTextureSize = 1f / (float)world.textureAtlasSizeInBlocks;
        float x = texturePos.x * normalizedBlockTextureSize;
        float y = texturePos.y * normalizedBlockTextureSize;
        float epsilon = 0.001f;

        uvs.Add(new Vector2(x + epsilon, y + epsilon));
        uvs.Add(new Vector2(x + epsilon, y + normalizedBlockTextureSize - epsilon));
        uvs.Add(new Vector2(x + normalizedBlockTextureSize - epsilon, y + epsilon));
        uvs.Add(new Vector2(x + normalizedBlockTextureSize - epsilon, y + normalizedBlockTextureSize - epsilon));
    }

    private bool CheckVoxel(Vector3Int pos)
    {
        if (pos.x < 0 || pos.x >= VoxelData.ChunkWidth ||
            pos.y < 0 || pos.y >= VoxelData.ChunkHeight ||
            pos.z < 0 || pos.z >= VoxelData.ChunkWidth)
        {
            Vector3 globalPos = chunkPosition + pos;
            byte neighborID = world.GetBlockID(globalPos);

            if (neighborID == 0) return false;
            return world.blockTypes[neighborID].isSolid;
        }

        byte localID = voxelMap[pos.x, pos.y, pos.z];
        if (localID == 0) return false;
        return world.blockTypes[localID].isSolid;
    }

    public byte GetVoxelID(Vector3Int localPos)
    {
        if (localPos.x < 0 || localPos.x >= VoxelData.ChunkWidth ||
            localPos.y < 0 || localPos.y >= VoxelData.ChunkHeight ||
            localPos.z < 0 || localPos.z >= VoxelData.ChunkWidth)
        {
            return 0;
        }
        return voxelMap[localPos.x, localPos.y, localPos.z];
    }

    private void CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }

    public void EditVoxel(Vector3Int localPos, byte newBlockID)
    {
        if (localPos.x < 0 || localPos.x >= VoxelData.ChunkWidth ||
            localPos.y < 0 || localPos.y >= VoxelData.ChunkHeight ||
            localPos.z < 0 || localPos.z >= VoxelData.ChunkWidth) return;

        voxelMap[localPos.x, localPos.y, localPos.z] = newBlockID;
        UpdateChunkMesh();
    }
}
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VoxelChunk : MonoBehaviour
{
    // Chunk dimensions
    private const int Width = 16;
    private const int Height = 16;
    private const int Depth = 16;

    // 3D grid representing block IDs (0 = Air, 1 = Dirt, 2 = Stone, etc.)
    private int[,,] _blocks = new int[Width, Height, Depth];

    private MeshFilter meshFilter;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        GenerateMockChunkData();
        GenerateMesh();
    }

    // Fills the chunk with a test pattern (e.g., a solid floor or random blocks)
    void GenerateMockChunkData()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int z = 0; z < Depth; z++)
            {
                for (int y = 0; y < Height; y++)
                {
                    // Generate a flat floor + some random noise blocks
                    if (y < 4)
                        _blocks[x, y, z] = 1; // Dirt floor
                    else if (y == 4 && x > 4 && x < 12 && z > 4 && z < 12)
                        _blocks[x, y, z] = 2; // Stone platform
                    else
                        _blocks[x, y, z] = 0; // Air
                }
            }
        }
    }

    // Helper to safely fetch block ID including boundary checks (Air if out of bounds)
    private int GetBlock(int x, int y, int z)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height || z < 0 || z >= Depth)
            return 0; // Out of bounds is treated as Air (shows outer faces)
        return _blocks[x, y, z];
    }

    public void GenerateMesh()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>(); // Standard UVs mapped per quad size

        // Sweep over all 3 axes (X = 0, Y = 1, Z = 2)
        for (int axis = 0; axis < 3; axis++)
        {
            // Determine working axes based on the sweep axis
            int uAxis = (axis + 1) % 3;
            int vAxis = (axis + 2) % 3;

            int[] chunkDims = new int[] { Width, Height, Depth };

            // Track positions along the dimensions
            int[] pos = new int[3];
            int[] dir = new int[3];
            dir[axis] = 1;

            // Mask holds the block ID and direction (positive vs negative face)
            // Storing integer block IDs allows merging faces of identical block types
            int[] mask = new int[chunkDims[uAxis] * chunkDims[vAxis]];

            // Step 1: Slice through the chunk along the current axis
            // We check from -1 to chunkDims[axis] to capture boundary faces properly
            for (pos[axis] = -1; pos[axis] < chunkDims[axis]; pos[axis]++)
            {
                int maskIdx = 0;

                // Build the face mask for this slice
                for (pos[vAxis] = 0; pos[vAxis] < chunkDims[vAxis]; pos[vAxis]++)
                {
                    for (pos[uAxis] = 0; pos[uAxis] < chunkDims[uAxis]; pos[uAxis]++)
                    {
                        // Compare the block at 'pos' vs the block next to it along the sweep axis
                        int blockCurrent = GetBlock(pos[0], pos[1], pos[2]);
                        int blockAdjacent = GetBlock(pos[0] + dir[0], pos[1] + dir[1], pos[2] + dir[2]);

                        bool isCurrentSolid = blockCurrent > 0;
                        bool isAdjacentSolid = blockAdjacent > 0;

                        if (isCurrentSolid == isAdjacentSolid)
                        {
                            // If both are solid or both are air, the face is hidden
                            mask[maskIdx++] = 0;
                        }
                        else if (isCurrentSolid)
                        {
                            // Back-face/Negative-face (Current is solid, adjacent is air)
                            // Encode block ID as a positive integer
                            mask[maskIdx++] = blockCurrent;
                        }
                        else
                        {
                            // Front-face/Positive-face (Current is air, adjacent is solid)
                            // Encode block ID as a negative integer to specify direction
                            mask[maskIdx++] = -blockAdjacent;
                        }
                    }
                }

                // Step 2: Greedy Sweep across the mask to merge matching adjacent faces
                int maskPos = 0;
                for (int v = 0; v < chunkDims[vAxis]; v++)
                {
                    for (int u = 0; u < chunkDims[uAxis];)
                    {
                        int maskValue = mask[maskPos + u];

                        if (maskValue != 0)
                        {
                            // Find the continuous width (u-dimension) of identical face types
                            int width = 1;
                            while (u + width < chunkDims[uAxis] && mask[maskPos + u + width] == maskValue)
                            {
                                width++;
                            }

                            // Find the continuous height (v-dimension) that matches this row's width & type
                            int height = 1;
                            bool canGrowHeight = true;
                            while (v + height < chunkDims[vAxis] && canGrowHeight)
                            {
                                for (int k = 0; k < width; k++)
                                {
                                    int nextRowMaskIdx = (maskPos + height * chunkDims[uAxis]) + u + k;
                                    if (mask[nextRowMaskIdx] != maskValue)
                                    {
                                        canGrowHeight = false;
                                        break;
                                    }
                                }
                                if (canGrowHeight) height++;
                            }

                            // We have a merged quad! Calculate physical 3D vertices
                            int[] quadW = new int[3]; quadW[uAxis] = width;
                            int[] quadH = new int[3]; quadH[vAxis] = height;

                            // Adjust position offset based on face orientation
                            int[] vertexPos = new int[3];
                            System.Array.Copy(pos, vertexPos, 3);
                            vertexPos[axis] += 1;

                            Vector3 v0, v1, v2, v3;

                            if (maskValue > 0) // Negative face direction
                            {
                                v0 = new Vector3(vertexPos[0], vertexPos[1], vertexPos[2]);
                                v1 = new Vector3(vertexPos[0] + quadW[0], vertexPos[1] + quadW[1], vertexPos[2] + quadW[2]);
                                v2 = new Vector3(vertexPos[0] + quadW[0] + quadH[0], vertexPos[1] + quadW[1] + quadH[1], vertexPos[2] + quadW[2] + quadH[2]);
                                v3 = new Vector3(vertexPos[0] + quadH[0], vertexPos[1] + quadH[1], vertexPos[2] + quadH[2]);
                            }
                            else // Positive face direction
                            {
                                v0 = new Vector3(vertexPos[0], vertexPos[1], vertexPos[2]);
                                v1 = new Vector3(vertexPos[0] + quadH[0], vertexPos[1] + quadH[1], vertexPos[2] + quadH[2]);
                                v2 = new Vector3(vertexPos[0] + quadW[0] + quadH[0], vertexPos[1] + quadW[1] + quadH[1], vertexPos[2] + quadW[2] + quadH[2]);
                                v3 = new Vector3(vertexPos[0] + quadW[0], vertexPos[1] + quadW[1], vertexPos[2] + quadW[2]);
                            }

                            // Add to mesh geometry data lists
                            int vertIndex = vertices.Count;
                            vertices.Add(v0); vertices.Add(v1); vertices.Add(v2); vertices.Add(v3);

                            triangles.Add(vertIndex); triangles.Add(vertIndex + 1); triangles.Add(vertIndex + 2);
                            triangles.Add(vertIndex); triangles.Add(vertIndex + 2); triangles.Add(vertIndex + 3);

                            // Texture UVs scale proportionally to width and height to prevent stretching
                            uvs.Add(new Vector2(0, 0));
                            uvs.Add(new Vector2(width, 0));
                            uvs.Add(new Vector2(width, height));
                            uvs.Add(new Vector2(0, height));

                            // Clear out the mask for the region we just merged so it isn't processed again
                            for (int l = 0; l < height; l++)
                            {
                                for (int k = 0; k < width; k++)
                                {
                                    mask[(maskPos + l * chunkDims[uAxis]) + u + k] = 0;
                                }
                            }

                            u += width;
                        }
                        else
                        {
                            u++;
                        }
                    }
                    maskPos += chunkDims[uAxis];
                }
            }
        }

        // 3. Construct and apply the final optimized mesh
        Mesh mesh = new Mesh();
        mesh.name = "GreedyChunkMesh";
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();

        mesh.RecalculateNormals(); // Crucial for lighting to map cleanly across merged planes
        meshFilter.mesh = mesh;
    }
}
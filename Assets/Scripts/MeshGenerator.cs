using UnityEngine;
using System.Collections.Generic;

public static class MeshGenerator
{
    static Vector3Int[] directions =
    {
        Vector3Int.forward,
        Vector3Int.back,
        Vector3Int.left,
        Vector3Int.right,
        Vector3Int.up,
        Vector3Int.down
    };

    public static Mesh GenerateMesh(Chunk chunk)
    {
        List<Vector3> verts = new();
        List<int> tris = new();

        for(int x=0;x<Chunk.Size;x++)
        {
            for(int y=0;y<Chunk.Size;y++)
            {
                for(int z=0;z<Chunk.Size;z++)
                {
                    Block block = chunk.GetBlock(x,y,z);

                    if(block.Type == BlockType.Air)
                        continue;

                    CheckFaces(
                        chunk,
                        x,y,z,
                        verts,
                        tris
                    );
                }
            }
        }

        Mesh mesh = new Mesh();

        mesh.vertices = verts.ToArray();
        mesh.triangles = tris.ToArray();

        mesh.RecalculateNormals();

        return mesh;
    }

    static void CheckFaces(
        Chunk chunk,
        int x,
        int y,
        int z,
        List<Vector3> verts,
        List<int> tris)
    {
        for(int i=0;i<6;i++)
        {
            Vector3Int dir = directions[i];

            int nx = x + dir.x;
            int ny = y + dir.y;
            int nz = z + dir.z;

            bool drawFace = false;

            if(nx < 0 ||
               nx >= Chunk.Size ||
               ny < 0 ||
               ny >= Chunk.Size ||
               nz < 0 ||
               nz >= Chunk.Size)
            {
                drawFace = true;
            }
            else
            {
                drawFace =
                    chunk.Blocks[nx,ny,nz].Type ==
                    BlockType.Air;
            }

            if(drawFace)
            {
                AddFace(
                    i,
                    new Vector3(x,y,z),
                    verts,
                    tris
                );
            }
        }
    }

    static void AddFace(
        int face,
        Vector3 pos,
        List<Vector3> verts,
        List<int> tris)
    {
        // one face at a time
        // simplified here
    }
}
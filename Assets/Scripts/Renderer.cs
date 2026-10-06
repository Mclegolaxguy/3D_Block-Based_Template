using System.CodeDom.Compiler;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class Renderer : MonoBehaviour
{
    // Chunk dimensions
    [SerializeField] private const int Width = 16;
    [SerializeField] private const int Height = 16;
    [SerializeField] private const int Depth = 16;

    //Mesh Filter
    private MeshFilter meshFilter;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
    }

    void GeneratedMesh(List<Block> data)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int axis = 0; axis < 3; axis++)
        {
            for (int blockIndex = 0; blockIndex < data.Count; blockIndex++)
            {
                //
            }
        }
    }
}

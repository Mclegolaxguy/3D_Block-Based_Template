using System.CodeDom.Compiler;
using System.Collections.Generic;
using UnityEngine;

public class Renderer : MonoBehaviour
{
    // Chunk dimensions
    [SerializeField] private const int Width = 16;
    [SerializeField] private const int Height = 16;
    [SerializeField] private const int Depth = 16;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        //
    }

    void GeneratedMesh()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
    }
}

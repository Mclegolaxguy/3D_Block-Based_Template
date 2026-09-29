using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class ChunkRenderer : MonoBehaviour
{
    public Chunk ChunkData;

    MeshFilter meshFilter;
    MeshCollider meshCollider;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshCollider = GetComponent<MeshCollider>();
    }

    public void BuildChunk()
    {
        Mesh mesh = MeshGenerator.GenerateMesh(ChunkData);

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
    }
}
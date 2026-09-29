using UnityEngine;

public class BlockInteraction : MonoBehaviour
{
    public Camera cam;
    public float reach = 5f;

    void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            BreakBlock();
        }

        if(Input.GetMouseButtonDown(1))
        {
            PlaceBlock();
        }
    }

    void BreakBlock()
    {
        if(Physics.Raycast(
            cam.transform.position,
            cam.transform.forward,
            out RaycastHit hit,
            reach))
        {
            ChunkRenderer chunk =
                hit.collider
                .GetComponent<ChunkRenderer>();

            if(chunk == null)
                return;

            Vector3 local =
                hit.point -
                chunk.transform.position;

            int x =
                Mathf.FloorToInt(local.x);

            int y =
                Mathf.FloorToInt(local.y);

            int z =
                Mathf.FloorToInt(local.z);

            chunk.ChunkData.SetBlock(
                x,y,z,
                new Block(BlockType.Air)
            );

            chunk.BuildChunk();
        }
    }

    void PlaceBlock()
    {
        if(Physics.Raycast(
            cam.transform.position,
            cam.transform.forward,
            out RaycastHit hit,
            reach))
        {
            ChunkRenderer chunk =
                hit.collider
                .GetComponent<ChunkRenderer>();

            Vector3 placePos =
                hit.point +
                hit.normal * 0.5f;

            placePos -= chunk.transform.position;

            int x =
                Mathf.FloorToInt(placePos.x);

            int y =
                Mathf.FloorToInt(placePos.y);

            int z =
                Mathf.FloorToInt(placePos.z);

            chunk.ChunkData.SetBlock(
                x,y,z,
                new Block(BlockType.Dirt)
            );

            chunk.BuildChunk();
        }
    }
}
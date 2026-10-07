using UnityEngine;
using UnityEngine.Events;

public class PlayerVoxelInteractor : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float reach = 5f;
    public WorldManager worldManager;
    public byte selectedBlockID = 1;

    [Header("Unity Events")]
    public UnityEvent<Vector3> OnBlockDestroyed;
    public UnityEvent<Vector3> OnBlockPlaced;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0)) // Left Click - Destroy
        {
            HandleBlockInteraction(true);
        }

        if (Input.GetMouseButtonDown(1)) // Right Click - Place
        {
            HandleBlockInteraction(false);
        }
    }

    private void HandleBlockInteraction(bool isDestroying)
    {
        Ray ray = cam.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, reach))
        {
            // Adjust hit point slightly into or out of the block depending on action
            Vector3 interactionPoint = hit.point;
            interactionPoint += isDestroying ? -hit.normal * 0.1f : hit.normal * 0.1f;

            Chunk targetChunk = worldManager.GetChunkFromVector3(interactionPoint);
            if (targetChunk != null)
            {
                // Convert world position to local chunk block coordinates
                Vector3Int localPos = new Vector3Int(
                    Mathf.FloorToInt(interactionPoint.x) - targetChunk.chunkPosition.x,
                    Mathf.FloorToInt(interactionPoint.y) - targetChunk.chunkPosition.y,
                    Mathf.FloorToInt(interactionPoint.z) - targetChunk.chunkPosition.z
                );

                if (isDestroying)
                {
                    targetChunk.EditVoxel(localPos, 0); // 0 is Air
                    OnBlockDestroyed.Invoke(interactionPoint);
                }
                else
                {
                    targetChunk.EditVoxel(localPos, selectedBlockID);
                    OnBlockPlaced.Invoke(interactionPoint);
                }
            }
        }
    }
}
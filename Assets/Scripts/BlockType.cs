using UnityEngine;

[System.Serializable]
public class BlockType
{
    public string blockName;
    public bool isSolid;

    [Header("Texture Coordinates (X, Y)")]
    // Coordinates assume a grid on your texture atlas (e.g., 0,0 is bottom-left)
    public Vector2Int backFaceTexture;
    public Vector2Int frontFaceTexture;
    public Vector2Int topFaceTexture;
    public Vector2Int bottomFaceTexture;
    public Vector2Int leftFaceTexture;
    public Vector2Int rightFaceTexture;

    // Matches the order of VoxelData.faceChecks
    public Vector2Int GetTextureID(int faceIndex)
    {
        switch (faceIndex)
        {
            case 0: return backFaceTexture;
            case 1: return frontFaceTexture;
            case 2: return topFaceTexture;
            case 3: return bottomFaceTexture;
            case 4: return leftFaceTexture;
            case 5: return rightFaceTexture;
            default: Debug.LogWarning("Invalid face index"); return Vector2Int.zero;
        }
    }
}
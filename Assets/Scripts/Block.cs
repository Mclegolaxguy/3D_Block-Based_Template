using UnityEngine;

public enum BlockType
{
    Air,
    Grass,
    Dirt,
    Stone
}

public struct Block
{
    public BlockType Type;

    public Block(BlockType type)
    {
        Type = type;
    }

    public bool IsSolid()
    {
        return Type != BlockType.Air;
    }
}
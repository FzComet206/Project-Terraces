using UnityEngine;

public static class Utils
{
    public static (int, int) ChunkIndexToChunkCoord(int index)
    {
        return (-1, -1);
    }

    public static int ChunkCoordToChunkIndex(int x, int y, int z)
    {
        return -1;
    }
    
    public static int PositionToChunkIndex(Vector3 position)
    {
        return -1;
    }
    
    public static (int, int) PositionToChunkCoord(Vector3 position)
    {
        return ChunkIndexToChunkCoord(PositionToChunkIndex(position));
    }
}


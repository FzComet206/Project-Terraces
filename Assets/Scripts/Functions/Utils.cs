using UnityEngine;

public static class Utils
{
    public static (int, int, int) ChunkIndexToCoord(int index)
    {
        return (-1, -1, -1);
    }

    public static int CoordToChunkIndex(int x, int y, int z)
    {
        return -1;
    }

    public static Vector3 ChunkIndexToPosition(int index)
    {
        return Vector3.zero;
    }
    
    public static int PositionToChunkIndex(Vector3 position)
    {
        return -1;
    }
}


using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class BrushSystem
{
    public enum BrushType
    {
        SmallSquare
    }
    
    public struct VoxelOperation
    {
        public int2 coord;
        public int localIndex;
        public int densityOperation;
        public VoxelOperation(int2 coord, int localIndex, int densityOperation)
        {
            this.coord = coord;
            this.localIndex = localIndex;
            this.densityOperation = densityOperation;
        }
    }

    public BrushType brushType;

    public BrushSystem()
    {
        // init brush system
        brushType = BrushType.SmallSquare;
    }

    public VoxelOperation[] EvaluateBrush(Vector3 position)
    {
        switch (brushType)
        {
            case BrushType.SmallSquare:

                VoxelOperation[] indexsAndChunksArray = new VoxelOperation[64];
                // find the coord
                // check if outside of bound
                // find local coord of index to the chunk
                // duplicate coord if necessary
                int counter = 0;

                for (int i = -2; i < 2; i++)
                {
                    for (int j = -2; j < 2; j++)
                    {
                        for (int k = -2; k < 2; k++)
                        {
                            int coordX = Mathf.FloorToInt((position.x + i) / 15f);
                            int coordZ = Mathf.FloorToInt((position.z + k) / 15f);
                            int2 coord = new int2(coordX, coordZ);
                            
                            int x = Mathf.RoundToInt(position.x) + i;
                            int y = Mathf.RoundToInt(position.y) + j;
                            int z = Mathf.RoundToInt(position.z) + k;

                            if (y > 255)
                            {
                                continue;
                            }
                            
                            int localX = Mathf.Abs(x % 15);
                            int localZ = Mathf.Abs(z % 15);
                            
                            int localIndex = localZ * 16 * 256 + y * 16 + localX;

                            string str = $"localZ: {localZ}, localX: {localX}, localY: {y}, localIndex: {localIndex} ===== on coord: {coordZ}, {coordX}";
                            Debug.Log(str);

                            VoxelOperation voxelOperation = new VoxelOperation(
                                coord,
                                localIndex,
                                5
                                );
                            
                            indexsAndChunksArray[counter] = voxelOperation;
                            counter++;
                        }
                    }
                }

                return indexsAndChunksArray;
                
            default:
                throw new IndexOutOfRangeException("????????????????");
        }
        // find corresponding int2 and get chunk from dictionary
        // subtract position from chunk position
        // get local point indexes
        
        // if overflow, get direction of overflow and find nearby chunk
        // get nearby chunk local indexes that changed
        
        // update data for each chunk
        // call compute shader to triangulate for each chunk
    }
}

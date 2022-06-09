using System;
using Unity.Mathematics;
using UnityEngine;

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

                VoxelOperation[] indexsAndChunksArray = new VoxelOperation[27];

                int x = Mathf.RoundToInt(position.x);
                int y = Mathf.RoundToInt(position.y);
                int z = Mathf.RoundToInt(position.z);

                int counter = 0;
                
                for (int i = -1; i < 2; i++)
                {
                    for (int j = -1; j < 2; j++)
                    {
                        for (int k = -1; k < 2; k++)
                        {
                            int _x = x + i;
                            int _y = y + j;
                            int _z = z + k;
                            
                            // find the coord
                            // check if outside of bound
                            // find local coord of index to the chunk
                            // duplicate coord if necessary
                            int coordX = Mathf.RoundToInt(_x / 15f);
                            int localX = _x % 15;

                            int coordZ = Mathf.RoundToInt(_z / 15f);
                            int localZ = _z % 15;
                            
                            // if _x, _z = 16 and 0, edge case

                            int2 coord = new int2(coordX, coordZ);

                            _y = Math.Clamp(_y, 0, 255);
                            localZ = Math.Clamp(localZ, 0, 15);
                            localX = Math.Clamp(localX, 0, 15);
                            
                            int localIndex = localZ * 16 * 256 + _y * 16 + localX;

                            VoxelOperation voxelOperation = new VoxelOperation(
                                coord,
                                localIndex,
                                1
                                );
                            
                            indexsAndChunksArray[counter] = voxelOperation;
                            counter++;
                            // find all corresponding chunks and which set of x y z belongs to which chunk
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

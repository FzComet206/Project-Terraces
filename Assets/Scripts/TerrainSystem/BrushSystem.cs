using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class BrushSystem
{
    // debug parameters
    public int localXDebug;
    public int localZDebug;
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

    public List<VoxelOperation> EvaluateBrush(Vector3 position)
    {
        switch (brushType)
        {
            case BrushType.SmallSquare:

                List<VoxelOperation> indexsAndChunksArray = new List<VoxelOperation>();
                // find the coord
                // check if outside of bound
                // find local coord of index to the chunk
                // duplicate coord if necessary
                for (int i = -1; i < 2; i++)
                {
                    for (int j = -1; j < 2; j++)
                    {
                        for (int k = -1; k < 2; k++)
                        {
                            int coordX = Mathf.FloorToInt((position.x + i) / 15f);
                            int coordZ = Mathf.FloorToInt((position.z + k) / 15f);
                            int2 coord = new int2(coordX, coordZ);
                            
                            int x = Mathf.FloorToInt(position.x) + i;
                            int y = Mathf.FloorToInt(position.y) + j;
                            int z = Mathf.FloorToInt(position.z) + k;

                            if (y > 255 || y < 0)
                            {
                                continue;
                            }

                            int localX = x % 15;
                            int localZ = z % 15;

                            if (x < 0)
                            {
                                localX = 15 - Math.Abs(localX);
                            }

                            if (z < 0)
                            {
                                localZ = 15 - Math.Abs(localZ);
                            }

                            this.localXDebug = localX;
                            this.localZDebug = localZ;
                            
                            int localIndex = localZ * 16 * 256 + y * 16 + localX;

                            string str = $"localZ: {localZ}, localX: {localX}, localY: {y}, localIndex: {localIndex} ===== on coord: {coordZ}, {coordX}";

                            VoxelOperation voxelOperation = new VoxelOperation(
                                coord,
                                localIndex,
                                10
                                );
                            
                            indexsAndChunksArray.Add(voxelOperation);
                            
                            // edge cases
                            int2 edgeCoord = coord;
                            int edgeLocalX = 0;
                            int edgeLocalZ = 0;
                            
                            // handle x edge
                            if (localX == 0)
                            {
                                edgeCoord = new int2(coord.x - 1, coord.y);
                                edgeLocalX = 15;
                                edgeLocalZ = localZ;
                            } else if (localX == 15)
                            {
                                edgeCoord = new int2(coord.x + 1, coord.y);
                                edgeLocalX = 0;
                                edgeLocalZ = localZ;
                            }
                            
                            // handle z edge
                            if (localZ == 0)
                            {
                                edgeCoord = new int2(coord.x, coord.y - 1);
                                edgeLocalZ = 15;
                            } else if (localZ == 15)
                            {
                                edgeCoord = new int2(coord.x, coord.y + 1);
                                edgeLocalZ = 0;
                            }
                            
                            // if it happens;
                            if (localX == 0 || localX == 15 || localZ == 0 || localZ == 15)
                            {
                                int edgeLocalIndex = edgeLocalZ * 16 * 256 + y * 16 + edgeLocalX;
                                VoxelOperation edgeVoxelOperation = new VoxelOperation(
                                    edgeCoord,
                                    edgeLocalIndex,
                                    10
                                    );
                                
                                indexsAndChunksArray.Add(edgeVoxelOperation);
                            }
                                
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

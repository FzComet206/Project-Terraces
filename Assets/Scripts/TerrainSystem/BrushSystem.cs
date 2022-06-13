using System;
using System.Collections.Generic;
using System.Windows.Forms;
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
                for (int i = -3; i < 4; i++)
                {
                    for (int j = -3; j < 4; j++)
                    {
                        for (int k = -3; k < 4; k++)
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

                            // this has to be mod 15 for correct alignment
                            int localX = x % 15;
                            int localZ = z % 15;
                            
                            // invert negative coords
                            if (x < 0)
                            {
                                localX = 14 - Math.Abs((x + 1) % 15);
                            }

                            if (z < 0)
                            {
                                localZ = 14 - Math.Abs((z + 1) % 15);
                            }

                            int localIndex = localZ * 16 * 256 + y * 16 + localX;

                            AddOp(coord, localIndex, indexsAndChunksArray);
                            
                            // ===================================================================
                            // if x == 15, then localX will be 0, thus ignoring index 15
                            // we do this by checking if localx == 0, the add index 15 to last chunk

                            int2 edgeCoord;
                            int edgeLocalIndex;

                            // check edge
                            if (localX == 0 && localZ == 0)
                            {
                                edgeCoord = new int2(coord.x - 1, coord.y - 1);
                                edgeLocalIndex = 15 * 16 * 256 + y * 16 + 15 ;
                                AddOp(edgeCoord, edgeLocalIndex, indexsAndChunksArray);

                                edgeCoord = new int2(coord.x - 1, coord.y);
                                edgeLocalIndex = localZ * 16 * 256 + y * 16 + 15 ;
                                AddOp(edgeCoord, edgeLocalIndex, indexsAndChunksArray);
                                
                                edgeCoord = new int2(coord.x, coord.y - 1);
                                edgeLocalIndex = 15 * 16 * 256 + y * 16 + localX;
                                AddOp(edgeCoord, edgeLocalIndex, indexsAndChunksArray);
                            } 
                            else if (localX == 0 )
                            {
                                edgeCoord = new int2(coord.x - 1, coord.y);
                                edgeLocalIndex = localZ * 16 * 256 + y * 16 + 15 ;
                                AddOp(edgeCoord, edgeLocalIndex, indexsAndChunksArray);
                            }
                            else if (localZ == 0 )
                            {
                                edgeCoord = new int2(coord.x, coord.y - 1);
                                edgeLocalIndex = 15 * 16 * 256 + y * 16 + localX;
                                AddOp(edgeCoord, edgeLocalIndex, indexsAndChunksArray);
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

    private static void AddOp(int2 edgeCoord, int edgeLocalIndex, List<VoxelOperation> indexsAndChunksArray)
    {
        VoxelOperation edgeVoxelOperation = new VoxelOperation(
            edgeCoord,
            edgeLocalIndex,
            10
        );

        indexsAndChunksArray.Add(edgeVoxelOperation);
    }
}

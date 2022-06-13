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
                            
                            // ===================================================================
                            // if x == 15, then localX will be 0, thus ignoring index 15
                            // we do this by checking if localx == 0, the add index 15 to last chunk

                            int2 edgeCoord;
                            int edgeLocalIndex;

                            int xFix = 15;
                            int zFIx = 15;

                            int xCond = 0;
                            int zCond = 0;

                            // check edge
                            if (localX == xCond && localZ == zCond)
                            {
                                Debug.Log("edge X + Z");

                                edgeCoord = new int2(coord.x - 1, coord.y - 1);
                                
                                edgeLocalIndex = zFIx * 16 * 256 + y * 16 + xFix;
                                
                            } else if (localX == xCond)
                            {
                                Debug.Log("edge X");

                                edgeCoord = new int2(coord.x - 1, coord.y);
                                edgeLocalIndex = localZ * 16 * 256 + y * 16 + xFix;
                                
                            }
                            else if (localZ == zCond)
                            {
                                Debug.Log("edge Z");

                                edgeCoord = new int2(coord.x, coord.y - 1);
                                edgeLocalIndex = zFIx * 16 * 256 + y * 16 + localX;
                            }
                            else
                            {
                                continue;
                            }
                            
                            VoxelOperation edgeVoxelOperation = new VoxelOperation(
                                    edgeCoord,
                                    edgeLocalIndex,
                                    10
                                    );
                            
                            indexsAndChunksArray.Add(edgeVoxelOperation);
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

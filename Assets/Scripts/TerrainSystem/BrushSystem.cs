using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public enum BrushShape
{
    Square,
    Sphere,
    special,
    smooth,
}

public enum OperationType
{
    set,
    add,
    special,
    water,
    smooth
}

public struct VoxelOperation
{
    public int2 coord;
    public int localIndex;
    public int densityOperation;
    public OperationType opType;
    public VoxelOperation(int2 coord, int localIndex, int densityOperation, OperationType opType)
    {
        this.coord = coord;
        this.localIndex = localIndex;
        this.densityOperation = densityOperation;
        this.opType = opType;
    }
}

public class BrushSystem
{
    // debug parameters
    public Dictionary<int2, Chunk> chunkDict;

    public int brushSize = 10;
    public int brushMultiplier = 2;

    public BrushShape brushShape;
    public OperationType opType;

    public BrushSystem()
    {
        // init brush system
        brushShape = BrushShape.Sphere;
        opType = OperationType.add;

        // when the brush type changes, set brush shape, op type, and multiplier
    }

    public List<VoxelOperation> EvaluateBrush(Vector3 position)
    {
        List<VoxelOperation> indexsAndChunksArray = new List<VoxelOperation>();

        for (int i = -brushSize; i < brushSize + 1; i++)
        {
            for (int j = -brushSize; j < brushSize + 1; j++)
            {
                for (int k = -brushSize; k < brushSize + 1; k++)
                {
                    int coordX = Mathf.FloorToInt((position.x + i) / 15f);
                    int coordZ = Mathf.FloorToInt((position.z + k) / 15f);
                    int2 coord = new int2(coordX, coordZ);
                    
                    int x = Mathf.FloorToInt(position.x) + i;
                    int y = Mathf.FloorToInt(position.y) + j;
                    int z = Mathf.FloorToInt(position.z) + k;
                    
                    // calculate density operation logics here

                    int densityOp = GetDensityOperationOfType(x, y, z, position);
                    
                    // end of density op

                    if (y > 254 || y < 1)
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

                    AddOp(coord, localIndex, indexsAndChunksArray, densityOp);
                    
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
                        AddOp(edgeCoord, edgeLocalIndex, indexsAndChunksArray, densityOp);

                        edgeCoord = new int2(coord.x - 1, coord.y);
                        edgeLocalIndex = localZ * 16 * 256 + y * 16 + 15 ;
                        AddOp(edgeCoord, edgeLocalIndex, indexsAndChunksArray, densityOp);
                        
                        edgeCoord = new int2(coord.x, coord.y - 1);
                        edgeLocalIndex = 15 * 16 * 256 + y * 16 + localX;
                        AddOp(edgeCoord, edgeLocalIndex, indexsAndChunksArray, densityOp);
                    } 
                    else if (localX == 0 )
                    {
                        edgeCoord = new int2(coord.x - 1, coord.y);
                        edgeLocalIndex = localZ * 16 * 256 + y * 16 + 15 ;
                        AddOp(edgeCoord, edgeLocalIndex, indexsAndChunksArray, densityOp);
                    }
                    else if (localZ == 0 )
                    {
                        edgeCoord = new int2(coord.x, coord.y - 1);
                        edgeLocalIndex = 15 * 16 * 256 + y * 16 + localX;
                        AddOp(edgeCoord, edgeLocalIndex, indexsAndChunksArray, densityOp);
                    }
                }
            }
        }
        return indexsAndChunksArray;
    }

    private void AddOp(int2 edgeCoord, int edgeLocalIndex, List<VoxelOperation> indexsAndChunksArray, int densityOp)
    {
        VoxelOperation edgeVoxelOperation = new VoxelOperation(
            edgeCoord,
            edgeLocalIndex,
            densityOp,
            opType
        );

        indexsAndChunksArray.Add(edgeVoxelOperation);
    }

    private int GetDensityOperationOfType(int x, int y, int z, Vector3 center)
    {
        switch (brushShape)
        {
            case BrushShape.Square:
                return 10;
            case BrushShape.Sphere:
                // max of mag is brushsize, min of mag is 0
                Vector3 curr = new Vector3(x, y, z);
                float mag = (curr - center).magnitude;
                float value = brushSize - mag;

                if (value < 0)
                {
                    value = 0;
                }
                
                return (int) value * brushMultiplier;
            
            case BrushShape.special:
                int origin = SampleLocalVoxel(x, y, z);

                if (0 < origin)
                {
                    return 1;
                }
                return -1;
            
            case BrushShape.smooth:
                int count = 0;
                int avg = 0;

                for (int i = -1; i < 2; i++)
                {
                    for (int j = -1; j < 2; j++)
                    {
                        for (int k = -1; k < 2; k++)
                        {
                            if (y + j < 255 && y + j >= 0)
                            {
                                count++;
                                avg += SampleLocalVoxel(x + i, y + j, z + k);
                            }
                        }
                    }
                }

                if (count == 0)
                {
                    return -1;
                }

                return avg / count;

            default:
                return -1;
        }
    }

    private int SampleLocalVoxel(int x, int y, int z)
    {
        int coordX = Mathf.FloorToInt(x / 15f);
        int coordZ = Mathf.FloorToInt(z / 15f);
        int2 coord = new int2(coordX, coordZ);
        
        int localX = x % 15;
        int localZ = z % 15;
        
        if (x < 0)
        {
            localX = 14 - Math.Abs((x + 1) % 15);
        }

        if (z < 0)
        {
            localZ = 14 - Math.Abs((z + 1) % 15);
        }

        int data = chunkDict[coord].data[localZ * 16 * 256 + y * 16 + localX];
        
        return data;
    }
}

using System.Collections.Generic;
using System.Diagnostics;
using Unity.Mathematics;
using UnityEngine;

public class FluidSystem
{
    public int[] lookUpDensity;
    public int[] lookUpFluid;
    public int[] simulateGrid;

    private int width = 105;
    private int offset = 45;
    private int[][] eightDir;
    
    public Vector3 playerPos;
    public List<int2> update;

    public Dictionary<int2, Chunk> chunksDict;

    public float threadSpeed;
    public FluidSystem(ChunkSystem chunkSystem)
    {
        lookUpDensity = new int[width * width * 256];
        lookUpFluid = new int[width * width * 256];
        simulateGrid = new int[width * width * 256];
        chunksDict = chunkSystem.chunksDict;

        update = new List<int2>();
    }

    public void Simulate()
    {
        update.Clear();
        HashSet<int2> _updateSet = new HashSet<int2>();

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        simulateGrid = new int[width * width * 256];
        
        // copy data
        Dictionary<int2, Chunk> cd = chunksDict;
        
        int2 origin = new int2((int)(playerPos.x / 15f), (int)(playerPos.z / 15f));
        int2 lastCoord = new int2(Mathf.FloorToInt(offset / 15f), Mathf.FloorToInt(offset / 15f)) + origin;

        int[] currDensity = cd[lastCoord].data;
        int[] currFluid = cd[lastCoord].fluid;

        for (int z = 0; z < width ; z++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < 256; y++)
                {
                    // 7 x 7 chunks , from -3 to 3, offset coord by 3 * 15 = 45
                    int2 relative = new int2(Mathf.FloorToInt((x - offset) / 15f), Mathf.FloorToInt((z - offset) / 15f));
                    int2 curr = origin + relative;

                    // do this to minimize dictionary lookup
                    if (!curr.Equals(lastCoord))
                    {
                        lastCoord = curr;
                        currDensity = cd[curr].data;
                        currFluid = cd[curr].fluid;
                    }
                    
                    int globalIndex = z * width * 256 + y * width + x;

                    int _z = z % 15;
                    int _x = x % 15;
                    int localIndex = _z * 16 * 256 + y * 16 + _x;

                    lookUpDensity[globalIndex] = currDensity[localIndex];
                    lookUpFluid[globalIndex] = currFluid[localIndex];
                }
            }
        }
        

        // simulate
        
        // if indexes are at global edge, water mask most be 0
        for (int z = 0; z < width ; z++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < 256; y++)
                {
                    int current = z * width * 256 + y * width + x;
                    
                    if (z % (width - 1) == 0 || x % (width - 1) == 0 || y % 255 == 0)
                    {
                        simulateGrid[current] = lookUpFluid[current];
                        continue;
                    }

                    int2 relative = new int2(Mathf.FloorToInt((x - offset) / 15f), Mathf.FloorToInt((z - offset) / 15f));
                    int2 curr = origin + relative;
                    
                    // indexes

                    if (lookUpFluid[current] > 0)
                    {
                        simulateGrid[current] = lookUpFluid[current];

                        // drop

                        for (int i = 1; i < 4; i++)
                        {
                            if (y - i < 0)
                            {
                                continue;
                            }

                            int below = z * width * 256 + (y - i) * width + x;

                            if (lookUpDensity[below] < 0 && lookUpFluid[below] == 0)
                            {
                                // the part that changes
                                simulateGrid[below] = 2;
                                if (!_updateSet.Contains(curr))
                                {
                                    _updateSet.Add(curr);
                                }
                            }
                        }
                    }


                    if (lookUpFluid[current] == 1)
                    {
                        // normal 
                        simulateGrid[current] = lookUpFluid[current];
                        for (int i = -3; i < 4; i++)
                        {
                            for (int j = -3; j < 4; j++)
                            {
                                int _x = x + i;
                                int _z = z + j;

                                if (_x < width && _x >= 0 && _z < width && _z >= 0)
                                {
                                    int dirIndex = _z * width * 256 + y * width + _x;
                                    if (lookUpDensity[dirIndex] < 0 && lookUpFluid[dirIndex] == 0)
                                    {
                                        simulateGrid[dirIndex] = 1;
                                        if (!_updateSet.Contains(curr))
                                        {
                                            _updateSet.Add(curr);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (lookUpFluid[current] == 2)
                    {
                        // volatile only spread if near ground
                        simulateGrid[current] = lookUpFluid[current];
                        int a = y - 1;

                        if (a >= 0)
                        {
                            int belowA = z * width * 256 + a * width + x;
                            if (lookUpDensity[belowA] > 0 )
                            {
                                for (int i = -3; i < 4; i++)
                                {
                                    for (int j = -3; j < 4; j++)
                                    {
                                        int _x = x + i;
                                        int _z = z + j;

                                        if (_x < width && _x >= 0 && _z < width && _z >= 0)
                                        {
                                            int dirIndex = _z * width * 256 + a * width + _x;
                                            if (lookUpDensity[dirIndex] < 0 && lookUpFluid[dirIndex] == 0 &&
                                                lookUpDensity[dirIndex] <= lookUpDensity[current])
                                            {
                                                simulateGrid[dirIndex] = 2;
                                                if (!_updateSet.Contains(curr))
                                                {
                                                    _updateSet.Add(curr);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        
        // put simulateGrid back into ds 
        // need to deal with edge cases for chunks
        lastCoord = new int2(Mathf.FloorToInt(offset / 15f), Mathf.FloorToInt(offset / 15f)) + origin;
        currFluid = cd[lastCoord].fluid;
        for (int z = 0; z < width ; z++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < 256; y++)
                {
                    int2 relative = new int2(Mathf.FloorToInt((x - offset) / 15f), Mathf.FloorToInt((z - offset) / 15f));
                    int2 curr = origin + relative;
                    
                    if (!curr.Equals(lastCoord))
                    {
                        lastCoord = curr;
                        currFluid = cd[curr].fluid;
                    }
                    
                    int globalIndex = z * width * 256 + y * width + x;
                    
                    // normal case
                    int _z = z % 15;
                    int _x = x % 15;
                    int localIndex = _z * 16 * 256 + y * 16 + _x;
                    currFluid[localIndex] = simulateGrid[globalIndex];
                    
                    // edge case
                    if (_z == 14 && _x == 14)
                    {
                        if (z + 1 < width && x + 1 < width)
                        {
                            int duplicateGlobal0 = (z + 1) * width * 256 + y * width + (x + 1);
                            int duplicateLocal0 = 15 * 16 * 256 + y * 16 + 15;
                            currFluid[duplicateLocal0] = simulateGrid[duplicateGlobal0];
                            
                            int duplicateGlobal1 = (z + 1) * width * 256 + y * width + x;
                            int duplicateLocal1 = 15 * 16 * 256 + y * 16 + _x;
                            currFluid[duplicateLocal1] = simulateGrid[duplicateGlobal1];
                            
                            int duplicateGlobal2 = z * width * 256 + y * width + (x + 1);
                            int duplicateLocal2 = _z * 16 * 256 + y * 16 + 15;
                            currFluid[duplicateLocal2] = simulateGrid[duplicateGlobal2];
                        }

                    } else if (_z == 14)
                    {
                        if (z + 1 < width)
                        {
                            int duplicateGlobal = (z + 1) * width * 256 + y * width + x;
                            int duplicateLocal = 15 * 16 * 256 + y * 16 + _x;
                            currFluid[duplicateLocal] = simulateGrid[duplicateGlobal];
                        }
                        
                    } else if (_x == 14)
                    {
                        if (x + 1 < width)
                        {
                            int duplicateGlobal = z * width * 256 + y * width + (x + 1);
                            int duplicateLocal = _z * 16 * 256 + y * 16 + 15;
                            currFluid[duplicateLocal] = simulateGrid[duplicateGlobal];
                        }
                    }
                }
            }
        }

        foreach (var i in _updateSet)
        {
            update.Add(i);
        }
        stopwatch.Stop();
        threadSpeed = stopwatch.ElapsedMilliseconds;
    }
}
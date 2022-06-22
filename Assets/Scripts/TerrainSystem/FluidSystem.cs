using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Policy;
using System.Transactions;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class FluidSystem
{
    public int[] lookUpDensity;
    public int[] lookUpFluid;
    public int[] simulateGrid;

    private int width = 105;
    private int offset = 45;
    
    public Vector3 playerPos;
    public Queue<int2> updateQueue;
    public HashSet<int2> updateSet;

    public Dictionary<int2, ChunkMemory> chunksDict;

    public float threadSpeed;
    public FluidSystem(ChunkSystem chunkSystem)
    {
        lookUpDensity = new int[width * width * 256];
        lookUpFluid = new int[width * width * 256];
        simulateGrid = new int[width * width * 256];
        chunksDict = chunkSystem.chunksDict;
        
        updateQueue = new Queue<int2>();
        updateSet = new HashSet<int2>();
    }

    public void Simulate()
    {
        updateQueue.Clear();
        updateSet.Clear();
        
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        simulateGrid = new int[width * width * 256];
        
        // copy data
        Dictionary<int2, ChunkMemory> cd = chunksDict;
        
        int2 origin = new int2((int)(playerPos.x / 15f), (int)(playerPos.z / 15f));
        int2 lastCoord = new int2(Mathf.FloorToInt(offset / 15f), Mathf.FloorToInt(offset / 15f)) + origin;

        int[] currDensity = cd[lastCoord].chunk.data;
        int[] currFluid = cd[lastCoord].chunk.fluid;

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
                        currDensity = cd[curr].chunk.data;
                        currFluid = cd[curr].chunk.fluid;
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
                    int2 relative = new int2(Mathf.FloorToInt((x - offset) / 15f), Mathf.FloorToInt((z - offset) / 15f));
                    int2 curr = origin + relative;
                    
                    // indexes
                    int current = z * width * 256 + y * width + x;
                    int below = z * width * 256 + (y - 1) * width + x;

                    // drop
                    if (y - 1 < 0)
                    {
                        continue;
                    }
                    
                    if (lookUpFluid[current] == 1)
                    {
                        simulateGrid[current] = 1;
                        if (lookUpDensity[below] < 0 && lookUpFluid[below] == 0)
                        {
                            // the part that changes
                            simulateGrid[below] = 1;
                            if (!updateSet.Contains(curr))
                            {
                                updateSet.Add(curr);
                                updateQueue.Enqueue(curr);
                            }
                        }
                    }
                }
            }
        }
        
        
        // put simulateGrid back into ds 
        // need to deal with edge cases for chunks
        lastCoord = new int2(Mathf.FloorToInt(offset / 15f), Mathf.FloorToInt(offset / 15f)) + origin;
        currFluid = cd[lastCoord].chunk.fluid;
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
                        currFluid = cd[curr].chunk.fluid;
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

        stopwatch.Stop();
        threadSpeed = stopwatch.ElapsedMilliseconds;
    }
}
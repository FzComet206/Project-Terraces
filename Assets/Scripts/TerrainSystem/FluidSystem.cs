using System.Collections.Generic;
using System.Diagnostics;
using Unity.Mathematics;
using UnityEngine;

public class FluidSystem
{
    public int[] lookUpDensity;
    public int[] lookUpFluid;
    public int[] simulateGrid;
    public HashSet<int2> _updateSet;

    private int width = 150;
    private int offset = 75;
    
    public Vector3 playerPos;
    public int2 origin;
    public List<int2> update;
    public Dictionary<int2, Chunk> chunksDict;

    public float threadSpeed;
    
    public FluidSystem(ChunkSystem chunkSystem)
    {
        lookUpDensity = new int[width * width * 256];
        lookUpFluid = new int[width * width * 256];
        simulateGrid = new int[width * width * 256];
        chunksDict = chunkSystem.chunksDict;

        _updateSet = new HashSet<int2>();
        update = new List<int2>();
    }

    public void Simulate()
    {
        _updateSet.Clear();
        update.Clear();

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        simulateGrid = new int[width * width * 256];
        
        // copy data
        Dictionary<int2, Chunk> cd = chunksDict;
        
        origin = new int2((int)(playerPos.x / 15f), (int)(playerPos.z / 15f));
        int2 lastCoord = new int2(Mathf.FloorToInt(offset / 15f), Mathf.FloorToInt(offset / 15f)) + origin;

        Chunk chunk;
        
        bool contains = cd.TryGetValue(lastCoord, out chunk);
        if (!contains)
        {
            return;
        }

        int[] currDensity = chunk.data;
        int[] currFluid = chunk.fluid;

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

                        bool ok = cd.TryGetValue(curr, out chunk);
                        if (!ok)
                        {
                            return;
                        }
                        currDensity = chunk.data;
                        currFluid = chunk.fluid;
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

        for (int z = 0; z < width ; z++)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < 256; y++)
                {
                    int current = z * width * 256 + y * width + x;

                    // indexes
                    int density = lookUpFluid[current];
                    if (density > 0)
                    {
                        if (density == 1)
                        {
                            simulateGrid[current] = density;
                            SearchAndAppendIndexes1(x, y, z);
                        }
                        else if (density == 2)
                        {
                            simulateGrid[current] = density;
                            SearchAndAppendIndexes2(x, y, z);
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

    private void SearchAndAppendIndexes2(int x, int y, int z)
    {
        if (y - 1 >= 0)
        {
            int below = z * width * 256 + (y - 1) * width + x;
            if (lookUpDensity[below] < 0)
            {
                simulateGrid[below] = 2;

                if (lookUpFluid[below] == 0)
                {
                    int2 relative = new int2(Mathf.FloorToInt((x - offset) / 15f),
                        Mathf.FloorToInt((z - offset) / 15f));
                    int2 curr = origin + relative;
                    for (int k = -1; k < 2; k++)
                    {
                        for (int l = -1; l < 2; l++)
                        {
                            int2 _curr = new int2(curr.x + k, curr.y + l);
                            _updateSet.Add(_curr);
                        }
                    }
                }
                return;
            }
        }
        
        int current = z * width * 256 + y * width + x;
        
        for (int i = -1; i < 2; i++)
        {
            for (int j = -1; j < 2; j++)
            {
                int a = x + j;
                int c = z + i;
                if (a >= 0 && a < width && c >= 0 && c < width)
                {
                    int index = c * width * 256 + y * width + a;
                    if (lookUpDensity[index] < 0 && lookUpDensity[index] <= lookUpDensity[current])
                    {
                        simulateGrid[index] = 2;
                        
                        if (lookUpFluid[index] == 0)
                        {
                            int2 relative = new int2(Mathf.FloorToInt((x - offset) / 15f),
                                Mathf.FloorToInt((z - offset) / 15f));
                            int2 curr = origin + relative;
                            for (int k = -1; k < 2; k++)
                            {
                                for (int l = -1; l < 2; l++)
                                {
                                    int2 _curr = new int2(curr.x + k, curr.y + l);
                                    _updateSet.Add(_curr);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    
    private void SearchAndAppendIndexes1(int x, int y, int z)
    {
        for (int i = -1; i < 2; i++)
        {
            for (int j = -1; j < 2; j++)
            {
                int a = x + j;
                int c = z + i;
                if (a >= 0 && a < width && c >= 0 && c < width)
                {
                    int index = c * width * 256 + y * width + a;
                    if (lookUpDensity[index] < 0)
                    {
                        simulateGrid[index] = 1;

                        if (lookUpFluid[index] == 0)
                        {
                            int2 relative = new int2(Mathf.FloorToInt((x - offset) / 15f),
                                Mathf.FloorToInt((z - offset) / 15f));
                            int2 curr = origin + relative;
                            for (int k = -1; k < 2; k++)
                            {
                                for (int l = -1; l < 2; l++)
                                {
                                    int2 _curr = new int2(curr.x + k, curr.y + l);
                                    _updateSet.Add(_curr);
                                }
                            }
                        }
                    }
                }
            }
        }
        
        if (y - 1 > 0)
        {
            int below = z * width * 256 + (y - 1) * width + x;
            if (lookUpDensity[below] < 0)
            {
                simulateGrid[below] = 2;

                if (lookUpFluid[below] == 0)
                {
                    int2 relative = new int2(Mathf.FloorToInt((x - offset) / 15f),
                        Mathf.FloorToInt((z - offset) / 15f));
                    int2 curr = origin + relative;
                    for (int k = -1; k < 2; k++)
                    {
                        for (int l = -1; l < 2; l++)
                        {
                            int2 _curr = new int2(curr.x + k, curr.y + l);
                            _updateSet.Add(_curr);
                        }
                    }
                }
            }
        }
    }
}
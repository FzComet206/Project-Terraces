using System.Collections.Generic;
using System.Diagnostics;
using Unity.Mathematics;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class FluidSystem
{
    public int[] lookUpDensity;
    public int[] lookUpFluid;
    public int[] simulateGrid;

    public Vector3 playerPos;
    private int width = 105;
    private int offset = 45;
    public Dictionary<int2, ChunkMemory> chunksDict;
    public FluidSystem(ChunkSystem chunkSystem)
    {
        lookUpDensity = new int[width * width * 256];
        lookUpFluid = new int[width * width * 256];
        simulateGrid = new int[width * width * 256];
        chunksDict = chunkSystem.chunksDict;
    }

    public void Simulate()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        
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
                    int globalIndex = z * width * 256 + y * width + x;

                    if (z % 15 == 0 || x % 15 == 0 || y % 255 == 0)
                    {
                        simulateGrid[globalIndex] = 0;
                    }
                    else
                    {
                        simulateGrid[globalIndex] = 1;
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
                    
                    int _z = z % 15;
                    int _x = x % 15;
                    int localIndex = _z * 16 * 256 + y * 16 + _x;
                    
                    currFluid[localIndex] = simulateGrid[globalIndex];
                }
            }
        }

        stopwatch.Stop();
        float ts = stopwatch.ElapsedMilliseconds;
        Debug.Log("fluid ended in " + ts);
    }
}
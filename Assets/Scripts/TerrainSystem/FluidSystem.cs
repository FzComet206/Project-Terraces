using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class FluidSystem: MonoBehaviour
{
    public int[] lookUpGrid;
    public int[] simulateGrid;

    private WorldManager worldManager;

    public void Start()
    {
        worldManager = FindObjectOfType<WorldManager>();
        
        lookUpGrid = new int[135 * 135 * 255];
        simulateGrid = new int[135 * 135 * 255];
    }

    public IEnumerator Simulate(Vector3 playerPos)
    {
        // copy data to simulator
        Dictionary<int2, ChunkMemory> cd = worldManager.chunkSystem.chunksDict;
        int indexesPerLocal = 15 * 15 * 255;

        int2 origin = new int2((int)(playerPos.x / 15f), (int)(playerPos.z / 15f));

        for (int z = 0; z < 105 ; z++)
        {
            for (int y = 0; y < 255; y++)
            {
                for (int x = 0; x < 105 ; x++)
                {
                    // 9 x 9 chunks , from -4 to 4, offset coord by 4 * 15 = 60
                    int2 relative = new int2(Mathf.FloorToInt((x - 60) / 15f), Mathf.FloorToInt((z - 60) / 15f));
                    int2 curr = origin + relative;
                    
                    int globalIndex = z * 135 * 255 + y * 135 + x;
                    int localIndex = globalIndex % indexesPerLocal;

                    lookUpGrid[globalIndex] = cd[curr].chunk.fluid[localIndex];
                }
            }
        }
        
        Array.Copy(lookUpGrid, simulateGrid, lookUpGrid.Length);
        
        // above calculation use 0.15 seconds

        
        // simulate data
        
        yield break;
    }
}
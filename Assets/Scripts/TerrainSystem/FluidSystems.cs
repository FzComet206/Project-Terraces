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
        // this is a coroutine that separates simulation into multiple frames
        
        
        // 9 frames for copy data to grid
        Dictionary<int2, ChunkMemory> cd = worldManager.chunkSystem.chunksDict;
        int indexesPerLocal = 15 * 15 * 255;

        int split = 135 * 135 * 256 / 9;
        int counter0 = 0;

        for (int z = 0; z < 135 ; z++)
        {
            for (int y = 0; y < 255; y++)
            {
                for (int x = 0; x < 135 ; x++)
                {
                    // 9 x 9 chunks , from -4 to 4, offset coord by 4 * 15 = 60
                    int2 curr = new int2((int)((x - 60) / 15f), (int)((z - 60) / 15f));
                    
                    int globalIndex = z * 135 * 255 + y * 135 + x;
                    int localIndex = globalIndex % indexesPerLocal;

                    lookUpGrid[globalIndex] = cd[curr].chunk.fluid[localIndex];
                    simulateGrid[globalIndex] = cd[curr].chunk.fluid[localIndex];
                    
                    counter0++;
                    if (counter0 % split == 0)
                    {
                        yield return new WaitForEndOfFrame();
                    }
                }
            }
        }

        // 27 frames for simulate data

        // 9 frames for copy data back
        
        // 27 frames for update mesh
        
        yield return new WaitForEndOfFrame();
    }
}

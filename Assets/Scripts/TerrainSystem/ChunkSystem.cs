using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class ChunkSystem
{
    // generated set
    // mesh queue
    // define coroutines for updates
    // culling

    private int renderDistance;
    public int RenderDistance
    {
        get => renderDistance;
        set => renderDistance = value;
    }

    public HashSet<int2> inQueue;
    public HashSet<int2> generated;
    public Queue<Chunk> queue;
    public ChunkSystem()
    {
        this.inQueue = new HashSet<int2>();
        this.generated = new HashSet<int2>(); 
        this.queue = new Queue<Chunk>();
    }

    public void UpdateNearbyChunks(Vector3 position)
    {
        // get all nearby non-generated and non-in-queue chunks into queue
        for (int i = -renderDistance; i <= renderDistance; i++)
        {
            for (int j = -renderDistance; j <= renderDistance; j++)
            {
                // round to actual coordinate
                int x = (int) (position.x / 15) + i;
                int z = (int) (position.z / 15) + j;
                
                int2 coord = new int2(x, z);

                if (!inQueue.Contains(coord) && !generated.Contains(coord))
                {
                    Chunk c = new Chunk(
                        (double) (x * 15f),
                        (double) (z * 15f),
                        x,
                        z);

                    inQueue.Add(coord);
                    queue.Enqueue(c);
                }
            }
        }
    }

    public Chunk GetCull(Vector3 position)
    {
        // find the farthest chunk in generated and return it
        throw new NotImplementedException();
    }
}

using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class ChunkMemory
{
    public GameObject meshChunk;
    public GameObject waterChunk;
    public Chunk chunk;
}
public class ChunkSystem
{
    private int renderDistance;
    public int RenderDistance
    {
        get => renderDistance;
        set => renderDistance = value;
    }

    public Dictionary<int2, Chunk> chunksDict;
    public HashSet<int2> inQueue;
    public HashSet<int2> generated;
    public Queue<Chunk> queue;
    public ChunkSystem()
    {
        this.chunksDict = new Dictionary<int2, Chunk>(); 
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
                int x = (int) (position.x / 15f) + i;
                int z = (int) (position.z / 15f) + j;
                
                int2 coord = new int2(x, z);

                if (!inQueue.Contains(coord) && !generated.Contains(coord))
                {
                    Chunk c = new Chunk(
                        (x * 15f),
                        (z * 15f),
                        x,
                        z);

                    inQueue.Add(coord);
                    queue.Enqueue(c);
                }
            }
        }
    }

    public void GetNearbyChunks(Vector3 position, ref Queue<int2> fluidUpdateQueue)
    {
        for (int i = -2; i <= 2; i++)
        {
            for (int j = - 2; j <= 2; j++)
            {
                // round to actual coordinate
                int x = (int) (position.x / 15f) + i;
                int z = (int) (position.z / 15f) + j;
                
                fluidUpdateQueue.Enqueue(new int2(x, z));
            }
        }
    }

    public int2 GetCull(Vector3 position)
    {
        // find the farthest chunk in generated and return it
        int x = (int)(position.x / 15f);
        int y = (int)(position.z / 15f);

        int2 farthest = new int2(0, 0);
        float distanceMax = 0;
        foreach (var key in chunksDict.Keys)
        {
            float dx = key.x - x;
            float dy = key.y - y;

            float distance = Mathf.Sqrt(dx * dx + dy * dy);
            if (distance > distanceMax)
            {
                distanceMax = distance;
                farthest = key;
            }
        }
        return farthest;
    }
}

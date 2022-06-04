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
    private DataTypes.ChunkInput chunkInput;

    public HashSet<Chunk> inQueue;
    public Dictionary<int2, Chunk> generated;
    public Queue<Chunk> queue;
    public ChunkSystem(DataTypes.ChunkInput chunkInput)
    {
        this.chunkInput = chunkInput;
        this.inQueue = new HashSet<Chunk>();
        this.generated = new Dictionary<int2, Chunk>();
        this.queue = new Queue<Chunk>();
    }

    public Chunk GetConfig(Vector3 position)
    {
        // get nearby chunk config and put it in queue
        // return the first chunk in queue
        throw new NotImplementedException();
    }

    public Chunk GetCull(Vector3 position)
    {
        // find the farthest chunk in generated and return it
        throw new NotImplementedException();
    }
}

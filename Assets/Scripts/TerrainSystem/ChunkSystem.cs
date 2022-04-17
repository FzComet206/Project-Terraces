
using System.Collections.Generic;

public class ChunkSystem
{
    // generated set
    // mesh queue
    // define coroutines for updates
    // culling
    private DataTypes.ChunkInput chunkInput;

    public HashSet<int> generated;
    public Queue<int> queue;
    public ChunkSystem(DataTypes.ChunkInput chunkInput)
    {
        this.chunkInput = chunkInput;
        this.generated = new HashSet<int>();
        this.queue = new Queue<int>();
    }
}

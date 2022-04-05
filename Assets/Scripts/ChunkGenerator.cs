using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public class ChunkGenerator : MonoBehaviour
{
    // serialized inputs
    [SerializeField] private Material chunkMaterial;
    [SerializeField] private GameObject pool;
    [SerializeField] private ComputeShader pointsShader;
    [SerializeField] private ComputeShader triangleShader;
    [SerializeField] private NoiseInput noiseInput; 
    
    // mesh inputs
    public int renderDistance;
    public int brushSize;
    public int blocky;
    
    // do not change
    private int width = 16;
    private int height = 256;
    private int numPoints;
    
    // ============================================= private variabls
    // for culling and updating
    private Controller player;
    private List<Chunk> activeChunks;
    private Queue<Chunk> processQueue;
    private HashSet<int> activeOrInQueue;

    // changed per new generation 
    private ComputeBuffer pointsBuffer;
    private ComputeBuffer pointsCopy;
    // changed per new generation and chunk update 
    private ComputeBuffer triangleBuffer;
    private ComputeBuffer triCountBuffer;
    private int totalVerticies;
    private int maxTriangleCount;
    private int chunkStep;

    private void Start()
    {
        player = FindObjectOfType<Controller>();
        processQueue = new Queue<Chunk>();
        activeChunks = new List<Chunk>();
        activeOrInQueue = new HashSet<int>();
        totalVerticies = 0;
        numPoints = width * width * height;
        maxTriangleCount = numPoints * 5;
        chunkStep = width - 1;
        renderDistance = Mathf.Clamp(renderDistance, 3, 10);
    }

    private void AddNearbyChunksToQueue()
    {
        for (int i = -renderDistance; i < renderDistance + 1; i++)
        {
            for (int j = -renderDistance; j < renderDistance + 1; j++)
            {
                Vector3 position = player.transform.position;
                int3 coord = ChunkCoordFromWorldPosition(position);
                coord.x += i;
                coord.z += j;
                int uniqueIndex = ChunkIndexFromChunkCoord(coord);
                
                // define chunks
                if (!activeOrInQueue.Contains(uniqueIndex))
                {
                    Chunk chunk = new Chunk(uniqueIndex, coord, chunkStep);
                    processQueue.Enqueue(chunk);
                    activeOrInQueue.Add(uniqueIndex);
                }
            }
        }
    }

    IEnumerator ProcessQueuePerFrame()
    {
        while (true)
        {
            if (processQueue.Count != 0)
            {
                Chunk c = processQueue.Dequeue();
                if (!c.active)
                {
                    c = GenerateChunk(c);
                    activeChunks.Add(c);
                    totalVerticies += c.NumVerts;
                }
            }

            yield return new WaitForFixedUpdate();
        }
    }

    public Chunk GenerateChunk(Chunk chunk)
    {
        CreateBuffer();
        
        DestroyBuffers();
        return new Chunk(1, new int3(1, 1, 1), 1);
    }

    private void DispatchPointShader(Vector3 startPosition)
    {
        
    }

    private void DispatchTriangleShader(Vector3 startPosition)
    {
        
    }

    int ChunkIndexFromWorldPosition(Vector3 position)
    {
        int x = Mathf.RoundToInt(position.x / chunkStep);
        int z = Mathf.RoundToInt(position.z / chunkStep);
        int y = Mathf.RoundToInt(position.y);
        return x + z * width + y * width * height;
    }
    
    int3 ChunkCoordFromWorldPosition(Vector3 position)
    {
        int x = Mathf.RoundToInt(position.x / chunkStep);
        int z = Mathf.RoundToInt(position.z / chunkStep);
        int y = Mathf.RoundToInt(position.y);
        return new int3(x, y, z);
    }

    private void CreateBuffer()
    {
        pointsBuffer = new ComputeBuffer (numPoints, sizeof (float) * 4);
        pointsCopy = new ComputeBuffer(numPoints, sizeof(float) * 4);
        triangleBuffer = new ComputeBuffer (maxTriangleCount, sizeof (float) * 3 * 3, ComputeBufferType.Append);
        triangleBuffer.SetCounterValue(0);
        triCountBuffer = new ComputeBuffer(1, sizeof(int));
    }

    private void DestroyBuffers()
    {
        pointsBuffer.Dispose();
        pointsCopy.Dispose();
        triangleBuffer.Dispose();
        triCountBuffer.Dispose();
    }

    int ChunkIndexFromChunkCoord(int3 coord)
    {
        return coord.x + coord.z * width + coord.y * width * height;
    }
    
    int[] BrushIndexesFromWorldPosition(Vector3 pos)
    {
        int[] indexes = new int[brushSize * brushSize * brushSize * 8];
        int c = 0;
        for (int i = -brushSize; i < brushSize; i++)
        {
            for (int j = -brushSize; j < brushSize; j++)
            {
                for (int k = -brushSize; k < brushSize; k++)
                {
                    int x = Mathf.RoundToInt(pos.x + i);
                    int y = Mathf.RoundToInt(pos.y + j);
                    int z = Mathf.RoundToInt(pos.z + k);

                    indexes[c] = x + z * width + y * width * height;
                    c++;
                }
            }
        }
        return indexes;
    }
    
    private void OnDestroy()
    {
        DestroyBuffers();
    }
} 

[System.Serializable]
public struct NoiseInput 
{
        [Header("Size Settings")]
        public int axisN;
    
        [Header("Fbm Settings")]
        public int octaves;
        public float lacunarity;
        public float gain;
        
        [Header("Noise Settings")]
        public float scale;
        public float isoLevel;
        public float weightMultiplier;
        public float noiseWeight;

        [Header("Modification Settings")]
        public float parameterX;
        public float parameterY;
        public float softFloor;
        public float softFloorWeight;

        public float hardFloor;
        public float hardFloorWeight;
}

public class Chunk
{
    public int index;
    public int3 coord;
    public bool active;

    public Vector3 startPosition; 
    
    // in memory required for modding
    private int numVerts;
    private float[] points;
    // references
    private Mesh mesh;
    private MeshCollider meshCollider;
    private MeshFilter meshFilter;

    public Chunk(int index, int3 coord, int chunkStep)
    {
        this.index = index;
        this.coord = coord;
        this.active = false;
        this.startPosition = new Vector3(coord.x * chunkStep, coord.y * chunkStep, coord.z * chunkStep);
    }

    public int NumVerts
    {
        get => numVerts;
        set => numVerts = value;
    }

    public float[] Points
    {
        get => points;
        set => points = value;
    }

    public Mesh Mesh
    {
        get => mesh;
        set => mesh = value;
    }

    public MeshCollider MeshCollider
    {
        get => meshCollider;
        set => meshCollider = value;
    }

    public MeshFilter MeshFilter
    {
        get => meshFilter;
        set => meshFilter = value;
    }
}

public class StoredChunkData
{
    public float[] points;
    public int[] waterMask;
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    [SerializeField] private Types.InputType mapInput;
    
    private Controller playerRef;
    private Chunks[] chunks;
    private Queue<Chunks> queue;

    private Vector3[] testBlock;
    private Queue<Vector3> testQueue;
    private HashSet<int> generated;
    
    // buffers
    private ComputeBuffer pointsBuffer;
    private ComputeBuffer v4Buffer;
    private ComputeBuffer triangleBuffer;
    private ComputeBuffer triCountBuffer;
    
    // other properties
    private int numPoints;
    private int numVoxels;
    private int numVoxelsPerAxis;
    private int numPointsPerAxis;
    private int maxTriangleCount;

    private void Start()
    {
        int d = mapInput.mapDivision; 
        numPointsPerAxis = mapInput.pointsPerChunkAxis;
        numPoints = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
        numVoxelsPerAxis = numPointsPerAxis - 1;
        numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
        maxTriangleCount = numVoxels * 5;
        
        playerRef = FindObjectOfType<Controller>();
        chunks = new Chunks[d * d * d];
        queue = new Queue<Chunks>();
        
        testQueue = new Queue<Vector3>();
        testBlock = new Vector3[d * d * d];
        generated = new HashSet<int>();
        
        GenerateAllChunksConfig();
        
        StartCoroutine(ChunkUpdate());
        StartCoroutine(MeshUpdate());
    }

    private IEnumerator ChunkUpdate()
    {
        while (true)
        {
            UpdateChunks();
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator MeshUpdate()
    {
        while (true)
        {
            if (queue.Count != 0)
            {
                Chunks c = queue.Dequeue();
                if (!c.active)
                {
                    c.active = true;
                    if (!c.generated)
                    {
                        CreateBuffers();
                        MakeMesh(c);
                    }
                }
            }
            yield return new WaitForFixedUpdate();
        }
    }

    void GenerateAllChunksConfig()
    {
        int d = mapInput.mapDivision;
        int s = mapInput.pointsPerChunkAxis;
        for (int i = 0; i < d; i++)
        {
            for (int j = 0; j < d; j++)
            {
                for (int k = 0; k < d; k++)
                {
                    float x = i - d / 2; 
                    float y = j - d / 2; 
                    float z = k - d / 2;
                    Vector3 p = new Vector3(x, y, z) * s;
                    int index = k + j * d + i * d * d;
                    testBlock[index] = p;

                    Chunks c = new Chunks(index, p);
                    chunks[index] = c;
                }
            }
        }
    }

    void UpdateChunks()
    {
        int d = mapInput.mapDivision;
        int s = mapInput.pointsPerChunkAxis;
        Vector3 p = playerRef.transform.position;

        int x = Mathf.RoundToInt(p.x / s + (d / 2f));
        int y = Mathf.RoundToInt(p.y / s + (d / 2f));
        int z = Mathf.RoundToInt(p.z / s + (d / 2f));
        
        for (int i = -2; i < 3; i++)
        {
            for (int j = -2; j < 3; j++)
            {
                for (int k = -2; k < 3; k++)
                {
                    int _i = x + i;
                    int _j = y + j;
                    int _k = z + k;
                    
                    int index = _k + _j * d + _i * d * d;

                    if (index < testBlock.Length && index >= 0 && !generated.Contains(index))
                    {
                        testQueue.Enqueue(testBlock[index]);
                        generated.Add(index);
                        queue.Enqueue(chunks[index]);
                    }
                }
            }
        }
    }

    public void MakeMesh(Chunks chunk)
    {
        ComputeHelper ch = FindObjectOfType<ComputeHelper>();
        
        // get noise value
        ch.DispatchNoiseBuffer(pointsBuffer, mapInput.noise, numPointsPerAxis, chunk.startPos, numPointsPerAxis / 8);
        
        // set data points back into buffer
        Vector4[] points = new Vector4[numPoints];
        pointsBuffer.GetData(points);
        v4Buffer.SetData(points);
        
        // brrr
        ch.DispatchTriangulateBuffer(v4Buffer, triangleBuffer, numPointsPerAxis, mapInput.noise.isoLevel, numPointsPerAxis / 8);

        // used for slicing triangle outputs
        ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
        int[] triC = new int[1];
        triCountBuffer.GetData(triC);
        int numT = triC[0];
        
        // get triangle and vert output
        Types.Tri[] tBuffer = new Types.Tri[numT];
        triangleBuffer.GetData(tBuffer, 0, 0, numT);
        
        // write vert and tri to property
        chunk.verticies = new Vector3[tBuffer.Length * 3];
        chunk.triangles = new int[tBuffer.Length * 3];
        
        for (int i = 0; i < tBuffer.Length; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                int id = 3 * i + j;
                chunk.verticies[id] = tBuffer[i][j];
                chunk.triangles[id] = id;
            }
        }
        
        pointsBuffer.Dispose();
        v4Buffer.Dispose();
        triangleBuffer.Dispose();
        triCountBuffer.Dispose();
        
        // construct mesh
        Mesh mesh = new Mesh();
        mesh.vertices = chunk.verticies;
        mesh.triangles = chunk.triangles;
        mesh.RecalculateNormals();
        
        chunk.chunk = new GameObject("Chunk " + chunk.index, 
            typeof(MeshFilter),
            typeof(MeshRenderer)
        );
        
        chunk.chunk.GetComponent<MeshFilter>().sharedMesh = mesh;
        chunk.chunk.GetComponent<MeshRenderer>().material = mapInput.meshMat;
        chunk.chunk.transform.parent = mapInput.pool;
    }

    private void CreateBuffers ()
    {
        // creates and reset buffers
        pointsBuffer = new ComputeBuffer (numPoints, sizeof (float) * 4);
        v4Buffer = new ComputeBuffer(numPoints, sizeof(float) * 4);
        triangleBuffer = new ComputeBuffer (maxTriangleCount, sizeof (float) * 3 * 3, ComputeBufferType.Append);
        triangleBuffer.SetCounterValue(0);
        triCountBuffer = new ComputeBuffer(1, sizeof(int));
    }
    private void OnDestroy()
    {
        if (pointsBuffer != null)
        {
            pointsBuffer.Dispose();
        }
        if (v4Buffer!= null)
        {
            v4Buffer.Dispose();
        }
        if (triangleBuffer != null)
        {
            triangleBuffer.Dispose();
        }
        if (triCountBuffer!= null)
        {
            triCountBuffer.Dispose();
        }
    }
}

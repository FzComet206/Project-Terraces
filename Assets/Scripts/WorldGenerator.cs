using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class WorldGenerator : MonoBehaviour
{
    [SerializeField] private Types.InputType mapInput;

    private Controller playerRef;
    
    private Chunks[] chunks;
    private List<Chunks> generated;
    private Queue<Chunks> queue;
    private HashSet<int> bruh;

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

    private int totalVerts;

    private void Start()
    {
        int d = mapInput.mapDivision;
        numPointsPerAxis = mapInput.pointsPerChunkAxis;
        numPoints = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
        numVoxelsPerAxis = numPointsPerAxis - 1;
        numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
        maxTriangleCount = numVoxels * 5;
        totalVerts = 0;

        playerRef = FindObjectOfType<Controller>();
        chunks = new Chunks[d * d * d];
        queue = new Queue<Chunks>();
        generated = new List<Chunks>();
        bruh = new HashSet<int>();

        GenerateAllChunksConfig();

        StartCoroutine(ChunkUpdate());
        StartCoroutine(MeshUpdate());
        StartCoroutine(Culling());
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
                    CreateBuffers();
                    MakeMesh(ref c);
                    
                    c.active = true;
                    
                    generated.Add(c);
                    totalVerts += c.verticies.Length;
                }
            }

            yield return new WaitForFixedUpdate();
        }
    }
    
    private IEnumerator Culling()
    {
        while (true)
        {
            if (totalVerts > 5000000)
            {
                // find farthest chunk 
                int farthest = 0;
                Vector3 p = playerRef.transform.position;
                for (int i = 0; i < generated.Count; i++)
                {
                    if ((generated[i].centerPos - p).magnitude > (generated[farthest].centerPos - p).magnitude)
                    {
                        farthest = i;
                    }
                }

                Chunks c = generated[farthest];
                generated.RemoveAt(farthest);
                
                totalVerts -= c.verticies.Length;
                Destroy(c.chunk);
                c.verticies = null;
                c.triangles = null;
                
                c.active = false;
                bruh.Remove(c.index);
            }

            yield return new WaitForFixedUpdate();
        }
    }

    void UpdateChunks()
    {
        int d = mapInput.mapDivision;
        int s = mapInput.pointsPerChunkAxis;
        Vector3 p = playerRef.transform.position;

        int x = Mathf.FloorToInt(p.x / s + d / 2f);
        int y = Mathf.FloorToInt(p.y / s + d / 2f);
        int z = Mathf.FloorToInt(p.z / s + d / 2f);
        
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
                    Chunks c = chunks[index];

                    if (index < chunks.Length && index >= 0 && !bruh.Contains(index))
                    {
                        bruh.Add(index);
                        queue.Enqueue(c);
                    }
                }
            }
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
                    // minus 1 is important
                    Vector3 start = new Vector3(x, y, z) * (s - 1);
                    Vector3 center = start + new Vector3(start.x / 2, start.y / 2, start.z / 2);
                    int index = k + j * d + i * d * d;

                    Chunks c = new Chunks(index, start, center);
                    chunks[index] = c;
                }
            }
        }
    }

    public void MakeMesh(ref Chunks chunk)
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
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.vertices = chunk.verticies;
        mesh.triangles = chunk.triangles;
        
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        chunk.chunk = new GameObject("Chunk " + chunk.index, 
            typeof(MeshFilter),
            typeof(MeshRenderer),
            typeof(MeshCollider)
        );
        
        chunk.chunk.GetComponent<MeshFilter>().mesh = mesh;
        chunk.chunk.GetComponent<MeshCollider>().sharedMesh = mesh;
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

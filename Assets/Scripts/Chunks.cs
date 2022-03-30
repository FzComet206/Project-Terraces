using System.Collections;
using UnityEngine;

public class Chunks
{
    // input configs 
    public int index;
    public Vector3 centerPos;
    public Vector3 startPos;
    private Types.InputType input;
    
    private int numPoints;
    private int numVoxels;
    private int numVoxelsPerAxis;
    private int numPointsPerAxis;
    private int maxTriangleCount;
    
    // states
    public bool active;
    public bool generated;
    
    // object datas
    public GameObject chunk;
    private Vector3[] verticies;
    private int[] triangles;

    private Material mat;
    private Transform parent;

    // buffer datas 
    private ComputeBuffer pointsBuffer;
    private ComputeBuffer v4Buffer;
    private ComputeBuffer triangleBuffer;
    private ComputeBuffer triCountBuffer;

    public Chunks(int index, Vector3 centerPos, Vector3 startPos, Types.InputType input, Material mat, Transform parent)
    {
        this.index = index;
        this.centerPos = centerPos;
        this.startPos = startPos;

        this.mat = mat;
        this.parent = parent;

        numPointsPerAxis = input.pointsPerChunkAxis;
        numPoints = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
        numVoxelsPerAxis = numPointsPerAxis - 1;
        numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
        maxTriangleCount = numVoxels * 5;
    }

    public void Refresh()
    {
        if (this.active)
        {
            if (!this.generated)
            {
                this.MakeMesh();
                this.generated = true;
            }
        }
        else
        {
            if (this.generated)
            {
                this.chunk.SetActive(false);
            } 
        }
    }

    private IEnumerator DeleteAfterTime()
    {
        yield return null;
    } 

    public void MakeMesh()
    {
        this.Triangulate();

        Mesh mesh = new Mesh();
        mesh.vertices = this.verticies;
        mesh.triangles = this.triangles;
        mesh.RecalculateNormals();
        
        this.chunk = new GameObject("Chunk " + index, 
            typeof(MeshFilter),
            typeof(MeshRenderer)
        );
        
        this.chunk.GetComponent<MeshFilter>().sharedMesh = mesh;
        this.chunk.GetComponent<MeshRenderer>().material = mat;
        this.chunk.transform.parent = parent;
    }

    private void Triangulate()
    {
        CreateBuffers();
        
        // get ch ref
        ComputeHelper ch = GameObject.FindObjectOfType<ComputeHelper>();
        
        // get noise value
        ch.DispatchNoiseBuffer(pointsBuffer, input.noise, numPointsPerAxis, startPos, numVoxelsPerAxis / 8);
        
        // set data points back into buffer
        Vector4[] points = new Vector4[numPoints];
        pointsBuffer.GetData(points);
        v4Buffer.SetData(points);
        
        // brrr
        ch.DispatchTriangulateBuffer(pointsBuffer, triangleBuffer, numPointsPerAxis, input.noise.isoLevel, numVoxelsPerAxis / 8);

        // used for slicing triangle outputs
        ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
        int[] triC = new int[1];
        triCountBuffer.GetData(triC);
        int numT = triC[0];
        
        // get triangle and vert output
        Types.Tri[] tBuffer = new Types.Tri[numT];
        triangleBuffer.GetData(tBuffer, 0, 0, numT);
        
        // write vert and tri to property
        this.verticies = new Vector3[tBuffer.Length * 3];
        this.triangles = new int[tBuffer.Length * 3];
        for (int i = 0; i < tBuffer.Length; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                int id = 3 * i + j;
                verticies[id] = tBuffer[i][j];
                triangles[id] = id;
            }
        }
        pointsBuffer.Dispose();
        v4Buffer.Dispose();
        triangleBuffer.Dispose();
        triCountBuffer.Dispose();
    }

    private void CreateBuffers () {

        pointsBuffer = new ComputeBuffer (numPoints, sizeof (float) * 4);
        v4Buffer= new ComputeBuffer(numPoints, sizeof(float) * 4);
        triangleBuffer = new ComputeBuffer (maxTriangleCount, sizeof (float) * 3 * 3, ComputeBufferType.Append);
        triCountBuffer = new ComputeBuffer(1, sizeof(int));
    }
}

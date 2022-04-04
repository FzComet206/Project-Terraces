using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class Generator : MonoBehaviour
{
    public Material meshMat;
    public GameObject meshObject;
    public ComputeShader pointsShader;
    public ComputeShader marchShader;

    public TestInput testInput;

    private int axisN;
    private int numPoints;
    
    private Types.Tri[] triangleArray;
    
    // update-able point array
    private float[] points;

    private Mesh meshRef;
    private MeshCollider meshCollider;
    private MeshFilter meshFilter;
    private Vector3[] verticies;
    private int[] triangles;

    [Range(1, 3)]
    public int brushSize;
    [Range(0, 1)]
    public float brushWeight;
    public bool startUpdate;

    public bool blocky = false;

    private void Start()
    {
        Generate();
    }

    private void Update()
    {
        if (startUpdate)
        {
            Generate();
        }
    }

    public void Generate()
    {
        axisN = testInput.axisN;
        numPoints = axisN * axisN * axisN;
        points = new float[numPoints];
        GetMainPointsData();
        GetMainTriangleData();
        GenerateMainMesh();
    }

    void GetMainPointsData()
    {
        pointsShader.SetInt("numPointsPerAxis", testInput.axisN);
        pointsShader.SetInt("octaves", testInput.octaves);
        pointsShader.SetFloat("lacunarity", testInput.lacunarity);
        pointsShader.SetFloat("gain", testInput.gain);
        pointsShader.SetFloat("scale", testInput.scale);
        pointsShader.SetFloat("weightMultiplier", testInput.weightMultiplier);
        pointsShader.SetFloat("parameterX", testInput.parameterX);
        pointsShader.SetFloat("parameterY", testInput.parameterY);
        pointsShader.SetFloat("noiseWeight", testInput.noiseWeight);
        pointsShader.SetFloat("softFloor", testInput.softFloor);
        pointsShader.SetFloat("softFloorWeight", testInput.softFloorWeight);
        pointsShader.SetFloat("hardFloor", testInput.hardFloor);
        pointsShader.SetFloat("hardFloorWeight", testInput.hardFloorWeight);

        ComputeBuffer cb = new ComputeBuffer(numPoints, sizeof(float));
        pointsShader.SetBuffer(0, "points", cb);
        int gs = testInput.axisN / 8;
        pointsShader.Dispatch(0, gs, gs, gs);
        cb.GetData(points);
        
        cb.Dispose();
    }

    void GetMainTriangleData()
    {
        ComputeBuffer pointsBuffer = new ComputeBuffer(numPoints, sizeof(float));
        ComputeBuffer triangleBuffer = new ComputeBuffer(numPoints * 5, sizeof (float) * 3 * 3, ComputeBufferType.Append);
        ComputeBuffer triCountBuffer = new ComputeBuffer(1, sizeof(int));
        triangleBuffer.SetCounterValue(0);
        
        pointsBuffer.SetData(points);
        
        marchShader.SetInt("numPointsPerAxis", testInput.axisN);
        marchShader.SetFloat("isoLevel", testInput.isoLevel);
        marchShader.SetBool("blocky", blocky);
        
        marchShader.SetBuffer(0, "points", pointsBuffer);
        marchShader.SetBuffer(0, "triangles", triangleBuffer);
        int gs = testInput.axisN / 8;
        marchShader.Dispatch(0, gs, gs, gs);
        
        ComputeBuffer.CopyCount(triangleBuffer, triCountBuffer, 0);
        int[] triCount = new int[1];
        triCountBuffer.GetData(triCount);
        int count = triCount[0];

        triangleArray = new Types.Tri[count];
        triangleBuffer.GetData(triangleArray, 0, 0, count);

        verticies = new Vector3[count * 3];
        triangles = new int[count * 3];
        for (int i = 0; i < triangleArray.Length; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                int id = 3 * i + j;
                verticies[id] = triangleArray[i][j];
                triangles[id] = id;
            }
        }
        
        pointsBuffer.Dispose();
        triangleBuffer.Dispose();
        triCountBuffer.Dispose();
    }

    public void GenerateMainMesh()
    {
        Mesh mesh = new Mesh();
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.vertices = verticies;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter = meshObject.GetComponent<MeshFilter>();
        meshCollider = meshObject.GetComponent<MeshCollider>();
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
        meshObject.GetComponent<MeshRenderer>().material = meshMat;

        meshRef = mesh;
    }

    public void UpdateMainmesh()
    {
        GetMainTriangleData();
        meshRef.Clear();
        meshRef.vertices = verticies;
        meshRef.triangles = triangles;

        meshFilter.mesh = meshRef;
        meshRef.RecalculateNormals();
        meshRef.RecalculateBounds();
        
        meshCollider.sharedMesh = null;
        meshCollider.sharedMesh = meshRef;
    }

    public void DeleteMesh()
    {
        DestroyImmediate(meshObject.GetComponent<MeshFilter>().sharedMesh);
        DestroyImmediate(meshObject.GetComponent<MeshCollider>().sharedMesh);
    }
    
    public void DispatchShaderWithPoints(Vector3 pos, bool add)
    {
        int[] indexes = IndexesFromWorldPosition(pos);
        int l = points.Length;
        foreach (var index in indexes)
        {
            if (index < l && index >= 0)
            {
                if (add)
                {
                    points[index] += brushWeight;
                }
                else
                {
                    points[index] -= brushWeight;
                }
            }
        }
        UpdateMainmesh();
    }


    int[] IndexesFromWorldPosition(Vector3 pos)
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
                    
                    indexes[c] = x + y * axisN + z * axisN * axisN;
                    c++;
                }
            }
        }

        return indexes;
    }
}



[System.Serializable]
public struct TestInput
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
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

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

    // compute buffers updated per frame
    private ComputeBuffer pointsBuffer;
    private ComputeBuffer triangleBuffer;
    private ComputeBuffer triCountBuffer;

    // update-able point array
    private float[] points;
    private Mesh meshRef;
    private MeshCollider meshCollider;
    private MeshFilter meshFilter;
    private Vector3[] verticies;
    private int[] triangles;

    [Range(1, 3)] public int brushSize;
    public bool startUpdate;
    public int brushWeight;
    public bool blocky = false;
    public bool blockyBrush = false;

    private Text fps;

    private void Start()
    {
        fps = FindObjectOfType<Text>();
        StartCoroutine(DisplayFPS());
        Generate();
    }

    private void Update()
    {

        if (startUpdate && !Application.isPlaying)
        {
            Generate();
        }
    }

    IEnumerator DisplayFPS()
    {
        while (true)
        {
            fps.text = String.Format("{0} FPS", Mathf.RoundToInt(1f / Time.deltaTime));
            yield return new WaitForSeconds(0.5f);
        }
    }

    public void Generate()
    {
        axisN = testInput.axisN;
        numPoints = axisN * axisN * axisN;
        points = new float[numPoints];
        InitBuffers();
        GetMainPointsData();
        GetMainTriangleData();
        GenerateMainMesh();

        if (!Application.isPlaying)
        {
            pointsBuffer.Dispose();
            triangleBuffer.Dispose();
            triCountBuffer.Dispose();
        }
    }

    void GetMainPointsData()
    {
        pointsShader.SetInt("numPointsPerAxis", testInput.axisN);
        pointsShader.SetInt("octaves", testInput.octaves);
        pointsShader.SetFloat("lacunarity", testInput.lacunarity);
        pointsShader.SetFloat("gain", testInput.gain);
        pointsShader.SetFloat("scale", testInput.scale);
        pointsShader.SetFloat("parameterX", testInput.parameterX);
        pointsShader.SetFloat("parameterY", testInput.parameterY);
        pointsShader.SetFloat("noiseWeight", testInput.noiseWeight);
        pointsShader.SetFloat("softFloor", testInput.softFloor);
        pointsShader.SetFloat("softFloorWeight", testInput.softFloorWeight);
        pointsShader.SetFloat("hardFloor", testInput.hardFloor);
        pointsShader.SetFloat("hardFloorWeight", testInput.hardFloorWeight);
        pointsShader.SetFloat("domainWrapWeight", testInput.domainWrapWeight);
        pointsShader.SetFloat("seed", (float) testInput.seed);

        pointsShader.SetBuffer(0, "points", pointsBuffer);
        int gs = testInput.axisN / 8;
        pointsShader.Dispatch(0, gs, gs, gs);
        pointsBuffer.GetData(points);
    }

    void GetMainTriangleData()
    {
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
        Debug.Log("yolo no freeze buff");
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
        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;
        meshObject.GetComponent<MeshRenderer>().material = meshMat;

        meshRef = mesh;
    }

    public void UpdateMainmesh()
    {
        GetMainTriangleData();
        meshRef.Clear();
        meshRef.SetVertices(verticies);
        meshRef.triangles = triangles;

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
                // brush system in the future

                if (!blockyBrush)
                {
                    // stable brush
                    float diff = Mathf.Abs(points[index] - testInput.isoLevel);
                    if (add)
                    {
                        points[index] += (diff + 1) * 0.1f * brushWeight;
                    }
                    else
                    {
                        points[index] -= (diff + 1) * 0.1f * brushWeight;
                    }
                }
                else
                {
                     float diff = Mathf.Abs(points[index] - testInput.isoLevel);
                     points[index] -= diff;
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
                    int x = Mathf.RoundToInt(pos.x) + i;
                    int y = Mathf.RoundToInt(pos.y) + j;
                    int z = Mathf.RoundToInt(pos.z) + k;
                    
                    indexes[c] = x + y * axisN + z * axisN * axisN;
                    c++;
                }
            }
        }

        return indexes;
    }

    private void InitBuffers()
    {
        pointsBuffer = new ComputeBuffer(numPoints, sizeof(float));
        triangleBuffer = new ComputeBuffer(numPoints * 5, sizeof (float) * 3 * 3, ComputeBufferType.Append);
        triCountBuffer = new ComputeBuffer(1, sizeof(int));
    }

    private void OnDestroy()
    {
        if (pointsBuffer != null)
        {
            pointsBuffer.Dispose();
        }
        if (triangleBuffer != null)
        {
            triangleBuffer.Dispose();
        }
        if (triCountBuffer != null)
        {
            triCountBuffer.Dispose();
        }
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
        public float noiseWeight;

        [Header("Modification Settings")]
        public float parameterX;
        public float parameterY;
        public float softFloor;
        public float softFloorWeight;

        public float hardFloor;
        public float hardFloorWeight;
        public float seed;

        public float domainWrapWeight;
}
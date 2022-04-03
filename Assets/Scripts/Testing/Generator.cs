using System;
using System.Collections;
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

    private int numPoints;
    private float[] points;
    private Types.Tri[] triangleArray;

    private Vector3[] verticies;
    private int[] triangles;

    public bool startUpdate;

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
        numPoints = testInput.axisN * testInput.axisN * testInput.axisN;
        points = new float[numPoints];
        GenerateMainMesh();
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
        GetMainPointsData();
        GetMainTriangleData();
        Mesh mesh = new Mesh();
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.vertices = verticies;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        meshObject.GetComponent<MeshFilter>().sharedMesh = mesh;
        meshObject.GetComponent<MeshCollider>().sharedMesh = mesh;
        meshObject.GetComponent<MeshRenderer>().material = meshMat;
    }

    public void DeleteMesh()
    {
        DestroyImmediate(meshObject.GetComponent<MeshFilter>().sharedMesh);
        DestroyImmediate(meshObject.GetComponent<MeshCollider>().sharedMesh);
    }
    
    void DispatchShaderWithPoints(float[] points)
    {
        
    }

    IEnumerator CaptureModifyDispatch()
    {
        return null;
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

}
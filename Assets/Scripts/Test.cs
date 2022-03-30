using UnityEngine;
using UnityEngine.Rendering;

public class Test : MonoBehaviour
{
    public ComputeShader pointCloud;
    public ComputeShader marchingCubes;

    public Material mat;

    private ComputeBuffer pointsBuffer;
    private ComputeBuffer v4buffer;
    private ComputeBuffer triangleBuffer;
    private ComputeBuffer triCount;
    
    private void Start()
    {
        int pointsPerAxis = 64;
        int numPoints = pointsPerAxis * pointsPerAxis * pointsPerAxis;
        
        pointsBuffer = new ComputeBuffer (numPoints, sizeof (float) * 4);
        v4buffer = new ComputeBuffer(numPoints, sizeof(float) * 4);
        triangleBuffer = new ComputeBuffer (numPoints * 5, sizeof (float) * 3 * 3, ComputeBufferType.Append);
        triCount = new ComputeBuffer(1, sizeof(int));
        
        pointCloud.SetInt("octaves", 3);
        pointCloud.SetInt("numPointsPerAxis", pointsPerAxis);
        pointCloud.SetFloat("scale", 40);
        pointCloud.SetFloat("lacunarity", 4f);
        pointCloud.SetFloat("gain", 0.2f);
        int h0 = pointCloud.FindKernel("GeneratePoints");
        pointCloud.SetBuffer(h0, "points", pointsBuffer);
        pointCloud.Dispatch(h0, pointsPerAxis / 8, pointsPerAxis / 8, pointsPerAxis / 8);

        Vector4[] points = new Vector4[numPoints];
        pointsBuffer.GetData(points);
        v4buffer.SetData(points);
    
        triangleBuffer.SetCounterValue(0);
        marchingCubes.SetInt("numPointsPerAxis", pointsPerAxis);
        marchingCubes.SetFloat("isoLevel", 0f);
        int h1 = marchingCubes.FindKernel("March");
        marchingCubes.SetBuffer(h1, "points", v4buffer);
        marchingCubes.SetBuffer(h1, "triangles", triangleBuffer);
        marchingCubes.Dispatch(h1, pointsPerAxis / 8, pointsPerAxis / 8, pointsPerAxis / 8);

        ComputeBuffer.CopyCount(triangleBuffer, triCount, 0);
        int[] triC = new int[1];
        triCount.GetData(triC);
        int numT = triC[0];

        Types.Tri[] tBuffer = new Types.Tri[numT];
        triangleBuffer.GetData(tBuffer, 0, 0, numT);
        
        Vector3[] verticies = new Vector3[tBuffer.Length * 3];
        int[] triangles = new int[tBuffer.Length * 3];
        
        for (int i = 0; i < tBuffer.Length; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                int id = 3 * i + j;
                verticies[id] = tBuffer[i][j];
                triangles[id] = id;
            }
        }

        GameObject obj = new GameObject("TestChunk", typeof(MeshFilter), typeof(MeshRenderer));
        Mesh mesh = new Mesh();
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.vertices = verticies;
        mesh.triangles = triangles;
        obj.GetComponent<MeshFilter>().mesh = mesh;
        obj.GetComponent<MeshRenderer>().material = mat;
        mesh.RecalculateNormals();
        
        pointsBuffer.Dispose();
        triangleBuffer.Dispose();
        triCount.Dispose();
    }

    private void OnDestroy()
    {
        if (pointsBuffer != null)
        {
            pointsBuffer.Dispose();
        }
        if (v4buffer != null)
        {
            v4buffer.Dispose();
        }
        if (triangleBuffer != null)
        {
            triangleBuffer.Dispose();
        }
        if (triCount != null)
        {
            triCount.Dispose();
        }
    }
}

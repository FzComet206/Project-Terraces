using UnityEngine;

public class MeshSystem
{
    private ComputeBuffer triangleBuffer;
    private ComputeBuffer triangleCountBuffer;
    
    private ComputeShader marchingCubes;
    public ComputeShader MarchingCubes
    {
        set => marchingCubes = value;
    }
    
    public MeshSystem()
    {
        int numPoints = 16 * 16 * 256;
        triangleBuffer = new ComputeBuffer(numPoints * 5, sizeof (float) * 3 * 3, ComputeBufferType.Append);
        triangleCountBuffer = new ComputeBuffer(1, sizeof(int));
    }

    public void GenerateMeshData(byte[] points)
    {
    }
}

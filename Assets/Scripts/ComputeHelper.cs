using Unity.Mathematics;
using UnityEngine;

public class ComputeHelper: MonoBehaviour
{
    [SerializeField] ComputeShader pointCloud;
    [SerializeField] ComputeShader marchingCubes;

    public void DispatchNoiseBuffer(ComputeBuffer points, Types.NoiseSettings ns, int numPointsPerAxis, Vector3 startPos, int groupSize)
    {
        pointCloud.SetInt("octaves", ns.octaves);
        pointCloud.SetInt("numPointsPerAxis", numPointsPerAxis);
        pointCloud.SetFloat("scale", ns.scale);
        pointCloud.SetFloat("lacunarity", ns.lacunarity);
        pointCloud.SetFloat("gain", ns.gain);
        pointCloud.SetVector("startPos", new Vector4(startPos.x, startPos.y, startPos.z, 0));
        int h = pointCloud.FindKernel("GeneratePoints");
        pointCloud.SetBuffer(h, "points", points);
        pointCloud.Dispatch(h, groupSize, groupSize, groupSize);
    }
    
    public void DispatchTriangulateBuffer(ComputeBuffer points, ComputeBuffer triangles, int numPointsPerAxis, float iso, int groupSize)
    {
        marchingCubes.SetInt("numPointsPerAxis", numPointsPerAxis);
        marchingCubes.SetFloat("isoLevel", iso);
        int h = marchingCubes.FindKernel("March");
        marchingCubes.SetBuffer(h, "points", points);
        marchingCubes.SetBuffer(h, "triangles", triangles);
        marchingCubes.Dispatch(h, groupSize, groupSize, groupSize);
    }
}

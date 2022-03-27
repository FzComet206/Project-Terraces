using UnityEngine;

public class ComputeHelper: MonoBehaviour
{
    [SerializeField] ComputeShader pointCloud;
    [SerializeField] ComputeShader marchingCubes;

    public Types.NoiseSettings ns;
    
    public void DispatchNoiseBuffer(ComputeBuffer points, Types.NoiseSettings ns, Vector3 startPos, int groupSize)
    {
        pointCloud.SetInt("octaves", ns.octaves);
        pointCloud.SetFloat("scale", ns.scale);
        pointCloud.SetFloat("lacunarity", ns.lacunarity);
        pointCloud.SetFloat("gain", ns.gain);
        
        pointCloud.SetVector("startPos", new Vector4(startPos.x, startPos.y, startPos.z, 0));
        
        pointCloud.Dispatch(pointCloud.FindKernel("GeneratePoints"), groupSize, groupSize, groupSize);
    }
    
    public void DispatchTriangulateBuffer(ComputeBuffer points, ComputeBuffer triangles, int groupSize)
    {
        marchingCubes.Dispatch(marchingCubes.FindKernel("GeneratePoints"), groupSize, groupSize, groupSize);
    }
    
}

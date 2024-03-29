using UnityEngine;

public class NoiseSystem 
{
    private DataTypes.NoiseLayerInput noiseOne;

    private ComputeBuffer points;
    private ComputeBuffer fluids;
    
    private ComputeShader pointsCompute;
    public ComputeShader PointsCompute
    {
        set => pointsCompute = value;
    }
    
    public NoiseSystem(DataTypes.NoiseLayerInput noiseOne)
    {
        this.noiseOne = noiseOne;
        points = new ComputeBuffer(16 * 16 * 256, sizeof(int));
        fluids = new ComputeBuffer(16 * 16 * 256, sizeof(int));
    }

    public (int[], int[]) DispatchPointBuffer(Chunk c)
    {
        int[] pointsArray = new int[16 * 16 * 256];
        int[] fluidArray = new int[16 * 16 * 256];
        
        float startX = c.startPositionX;
        float startZ = c.startPositionZ;
        
        pointsCompute.SetFloat("startX", startX);
        pointsCompute.SetFloat("startZ", startZ);

        pointsCompute.SetInt("seaLevel", 40);
        pointsCompute.SetInt("octaves", noiseOne.octaves);
        pointsCompute.SetFloat("lacunarity", noiseOne.lacunarity);
        pointsCompute.SetFloat("gain", noiseOne.gain);
        pointsCompute.SetFloat("scale", noiseOne.scale);
        pointsCompute.SetFloat("parameterX", noiseOne.parameterX);
        pointsCompute.SetFloat("parameterY", noiseOne.parameterY);
        pointsCompute.SetFloat("noiseWeight", noiseOne.noiseWeight);
        pointsCompute.SetFloat("softFloor", noiseOne.softFloorHeight);
        pointsCompute.SetFloat("softFloorWeight", noiseOne.softFloorWeight);
        pointsCompute.SetFloat("hardFloor", noiseOne.hardFloorHeight);
        pointsCompute.SetFloat("hardFloorWeight", noiseOne.hardFloorWeight);
        pointsCompute.SetFloat("domainWrapWeight", noiseOne.domainWrapWeight);
        pointsCompute.SetInt("seed", noiseOne.seed);

        pointsCompute.SetBuffer(0, "points", points);
        pointsCompute.SetBuffer(0, "fluids", fluids);
        pointsCompute.Dispatch(0, 4, 64, 4);
        points.GetData(pointsArray);
        fluids.GetData(fluidArray);
        
        return (pointsArray, fluidArray);
    }

    public void Destroy()
    {
        points.Dispose();
        fluids.Dispose();
    }
}

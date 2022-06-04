using Unity.Mathematics;
using UnityEngine;

public class NoiseSystem
{
    private DataTypes.NoiseLayerInput noiseOne;
    private DataTypes.NoiseLayerInput noiseTwo;

    private ComputeBuffer points;
    
    public NoiseSystem(DataTypes.NoiseLayerInput noiseOne, DataTypes.NoiseLayerInput noiseTwo)
    {
        this.noiseOne = noiseOne;
        this.noiseTwo = noiseTwo;
        points = new ComputeBuffer(16 * 16 * 256, sizeof(float) / 2);
    }

    public half[] DispatchPointBuffer(Vector3 startPosition)
    {
        return new half[1];
    }

    public void Destroy()
    {
        points.Dispose();
    }
}

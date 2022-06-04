using UnityEngine;

public class NoiseSystem
{
    private DataTypes.NoiseLayerInput noiseOne;

    private ComputeBuffer points;
    
    public NoiseSystem(DataTypes.NoiseLayerInput noiseOne)
    {
        this.noiseOne = noiseOne;
        points = new ComputeBuffer(16 * 16 * 256, sizeof(byte));
    }

    public byte[] DispatchPointBuffer(Vector3 startPosition)
    {
        return new byte[1];
    }

    public void Destroy()
    {
        points.Dispose();
    }
}

using UnityEngine;

public static class DataTypes 
{
    [System.Serializable]
    public struct ControllerInput
    {
        public float walkSpeed;
        public float rotateSpeed;
        public float sprintFactor;
        public float jumpStrength;
    }
    
    [System.Serializable]
    public struct NoiseLayerInput
    {
        [Range(100, 1000)]
        public int scale;
        [Range(1, 6)]
        public int octaves;
        [Range(1, 6)]
        public float lacunarity;
        [Range(0, 1)]
        public float gain;
        [Range(1, 256)] 
        public int noiseWeight;
        [Range(1, 100)]
        public float parameterX;
        [Range(0.0001f, 5)]
        public float parameterY;
        [Range(0, 128)]
        public float softFloorHeight;
        [Range(0.0001f, 5)]
        public float softFloorWeight;
        [Range(0, 128)]
        public float hardFloorHeight;
        [Range(0.0001f, 64)]
        public float hardFloorWeight;
        [Range(0f, 1f)] 
        public float domainWrapWeight;
        public int seed;
    }
    
    [System.Serializable]
    public struct FluidInput 
    {
    }
    
    [System.Serializable]
    public struct ChunkInput
    {
        public int renderDistance;
        public int chunksPerFrame;
        public Material meshMaterial;
        public Transform meshParent;
    }
    
    [System.Serializable]
    struct BrushInput
    {
        public int brushSize;
        public int brushStrength;
    }
}

public class Chunk
{
    public float startPositionX;
    public float startPositionZ;
    public int coordX;
    public int coordZ;

    public Chunk(float startPositionX, float startPositionZ, int coordX, int coordZ)
    {
        this.startPositionX = startPositionX;
        this.startPositionZ = startPositionZ;
        this.coordX = coordX;
        this.coordZ = coordZ;
    }

    private Chunk posX;
    public Chunk PosX
    {
        get => posX;
        set => posX = value;
    }
    
    private Chunk negX;
    public Chunk NegX
    {
        get => negX;
        set => negX = value;
    }
    
    private Chunk posZ;
    public Chunk PosZ
    {
        get => posZ;
        set => posZ = value;
    }
    
    private Chunk negZ;
    public Chunk NegZ
    {
        get => negZ;
        set => negZ = value;
    }

    private bool active;
    public bool Active
    {
        get => active;
        set => active = value;
    }
    
    private bool water;
    public bool Water
    {
        get => water;
        set => water = value;
    }

    private int[] data;
    public int[] Data
    {
        get => data;
        set => data = value;
    }

    private Texture2D shaderInput;
    public Texture2D ShaderInput
    {
        get => shaderInput;
        set => shaderInput = value;
    }
    
    public byte[] SerializeSelf()
    {
        return new byte[1];
    }

    public void InitSelfFromData()
    {
        
    }
}

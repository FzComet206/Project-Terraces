using System;
using Unity.Mathematics;
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
        public int Octaves;
        [Range(1, 6)]
        public float lacunarity;
        [Range(1, 2)]
        public float gain;
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
    }
    
    [System.Serializable]
    public struct MeshInput 
    {
    }
    
    [System.Serializable]
    public struct FluidInput 
    {
    }
    
    [System.Serializable]
    public struct ChunkInput
    {
        public int maxViewDistance;
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
    private int3 startPosition;
    public int3 StartPosition
    {
        get => startPosition;
        set => startPosition = value;
    }
    
    private int2 coord;
    public int2 Coord
    {
        get => coord;
        set => coord = value;
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

    private byte[] data;
    public byte[] Data
    {
        get => data;
        set => data = value;
    }

    private byte[] objects;
    public byte[] Objects
    {
        get => objects;
        set => objects = value;
    }
    public byte[] SerializeSelf()
    {
        return new byte[1];
    }

    public void InitSelfFromData()
    {
        
    }
}

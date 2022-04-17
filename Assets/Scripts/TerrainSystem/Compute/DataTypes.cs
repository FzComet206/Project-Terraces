using UnityEngine;

public static class DataTypes 
{
    [System.Serializable]
    public struct ControllerInput 
    {
    }
    
    [System.Serializable]
    public struct NoiseLayerInput 
    {
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
    }
}

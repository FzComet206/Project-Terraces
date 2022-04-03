using System;
using UnityEngine;


public static class Types 
{
    [System.Serializable]
    public struct InputType
    {
        public int pointsPerChunkAxis;
        [Range(10, 100)]
        public int mapDivision;
        
        public NoiseSettings noise;
        
        public Material meshMat;
        public Transform pool;
    }
    
    [System.Serializable]
    public struct NoiseSettings
    {
        public int octaves;
        public float lacunarity;
        public float gain;
        public float scale;
        public float isoLevel;
    }

    public struct Tri{
        #pragma warning disable 649 // disable unassigned variable warning
        public Vector3 a;
        public Vector3 b;
        public Vector3 c;
        public Vector3 this [int i] {
            get {
                switch (i) {
                    case 0:
                        return a;
                    case 1:
                        return b;
                    default:
                        return c;
                }
            }
        }
    }
    
}

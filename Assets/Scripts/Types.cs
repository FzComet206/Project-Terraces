using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using Vector4 = System.Numerics.Vector4;


public static class Types 
{
    [System.Serializable]
    public struct InputType
    {
        public int pointsPerChunkAxis;
        public int mapDivision;
        public Material meshMat;
        public Material boundMat;
        public Transform pool;

        public NoiseSettings noise;
    }
    
    [System.Serializable]
    public struct NoiseSettings
    {
        public int octaves;
        public float lacunarity;
        public float gain;
        public float scale;
        [Range(-1, 1)] public float isoLevel;
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

using UnityEditor;
using UnityEngine;

public static class Types 
{
    [System.Serializable]
    public struct InputType
    {
        public int mapSize;
        public int mapDivision;
        public Material boundMat;
        public GameObject pool;
    }
}

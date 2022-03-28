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
        public int mapSize;
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

    struct Tri{
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
    
    public class Chunks
    {
        // input configs 
        public int index;
        public Vector3 centerPos;
        public int3 startPos;
        private InputType input;
        
        private int numPoints;
        private int numVoxels;
        private int numVoxelsPerAxis;
        private int numPointsPerAxis;
        private int maxTriangleCount;
        
        // states
        public bool active;
        private bool generated;
        
        // object datas
        public GameObject chunk;
        private Vector3[] verticies;
        private int[] triangles;

        // buffer datas 
        private ComputeBuffer pointsBuffer;
        private ComputeBuffer triangleBuffer;
        
        public Chunks(int index, Vector3 centerPos, int3 startPos, InputType input)
        {
            this.index = index;
            this.centerPos = centerPos;
            this.startPos = startPos;

            numPointsPerAxis = input.mapSize / input.mapDivision;
            numPoints = numPointsPerAxis * numPointsPerAxis * numPointsPerAxis;
            numVoxelsPerAxis = numPointsPerAxis - 1;
            numVoxels = numVoxelsPerAxis * numVoxelsPerAxis * numVoxelsPerAxis;
            maxTriangleCount = numVoxels * 5;
        }

        public void Refresh()
        {
            if (this.active)
            {
                if (!this.generated)
                {
                    this.MakeMesh();
                    this.generated = true;
                }
            }
            else
            {
                if (this.generated)
                {
                    this.chunk.SetActive(false);
                } 
            }
        }

        private IEnumerator DeleteAfterTime()
        {
            yield return null;
        } 

        public void MakeMesh()
        {
            this.Triangulate();

            Mesh mesh = new Mesh();
            mesh.vertices = this.verticies;
            mesh.triangles = this.triangles;
            mesh.RecalculateNormals();
            
            this.chunk = new GameObject("Chunk", new Type[]
            {
                typeof(MeshFilter),
                typeof(MeshRenderer),
            } );
            
            this.chunk.GetComponent<MeshFilter>().sharedMesh = mesh;
            this.chunk.GetComponent<MeshRenderer>().material = input.meshMat;
            this.chunk.transform.parent = input.pool;
        }

        private void Triangulate()
        {
            CreateBuffers();
            
            ComputeHelper ch = GameObject.FindObjectOfType<ComputeHelper>();
            
            ch.DispatchNoiseBuffer(pointsBuffer, input.noise, startPos, numVoxelsPerAxis / 8);
            ch.DispatchTriangulateBuffer(pointsBuffer, triangleBuffer, numPointsPerAxis, input.noise.isoLevel, numVoxelsPerAxis / 8);

            // copy to arr and set obj
            int numT = triangleBuffer.count;
            Tri[] tBuffer = new Tri[numT];
            
            triangleBuffer.GetData(tBuffer, 0, 0, numT);
            
            Debug.Log(tBuffer[123].a);
            Debug.Log(tBuffer[123].b);
            Debug.Log(tBuffer[123].c);

            this.verticies = new Vector3[tBuffer.Length * 3];
            this.triangles = new int[tBuffer.Length * 3];
            
            for (int i = 0; i < tBuffer.Length; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    int id = 3 * i + j;
                    this.verticies[id] = tBuffer[i][j];
                    this.triangles[id] = id;
                }
            }
            
            pointsBuffer.Dispose();
            triangleBuffer.Dispose();
        }

        private void CreateBuffers () {

            pointsBuffer = new ComputeBuffer (numPoints, sizeof (float) * 4);
            triangleBuffer = new ComputeBuffer (maxTriangleCount, sizeof (float) * 3 * 3, ComputeBufferType.Append);
        }
    }
}

using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;


public static class Types 
{
    [System.Serializable]
    public struct InputType
    {
        public int mapSize;
        public int mapDivision;
        public Material meshMat;
        public Material boundMat;
        public GameObject pool;

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
    
    public class Chunks
    {
        // input configs 
        public int index;
        public Vector3 centerPos;
        public int3 startPos;
        private InputType input;
        
        private int numPoints;
        private int numVoxels;
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
            int numVoxelsPerAxis = numPointsPerAxis - 1;
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
            
            this.chunk = new GameObject("Chunk", new Type[]
            {
                typeof(MeshFilter),
                typeof(MeshRenderer),
            } );
            
            this.chunk.GetComponent<MeshFilter>().sharedMesh = mesh;
            this.chunk.GetComponent<MeshRenderer>().material = input.meshMat;
            this.chunk.transform.parent = input.pool.transform;
        }

        private void Triangulate()
        {
            CreateBuffers();
            
            ComputeHelper ch = GameObject.FindObjectOfType<ComputeHelper>();
            
            ch.DispatchNoiseBuffer(pointsBuffer, input.noise, startPos, 8);
            ch.DispatchTriangulateBuffer(pointsBuffer, triangleBuffer, numPointsPerAxis, input.noise.isoLevel, 8);

            // copy to arr and set obj
            this.verticies = new Vector3[pointsBuffer.count];
            this.triangles = new int[triangleBuffer.count];
            pointsBuffer.GetData(this.verticies);
            triangleBuffer.GetData(this.triangles);
            
            ReleaseBuffers();
        }

        private void CreateBuffers () {

            pointsBuffer = new ComputeBuffer (numPoints, sizeof (float) * 4);
            triangleBuffer = new ComputeBuffer (maxTriangleCount, sizeof (float) * 3 * 3, ComputeBufferType.Append);
        }

        private void ReleaseBuffers () {
            if (pointsBuffer != null)
            {
                pointsBuffer.Release();
            }
            if (triangleBuffer != null)
            {
                triangleBuffer.Release();
            }
        }
    }
}

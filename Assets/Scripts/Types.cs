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

        public NoiseSettings noise;
    }
    
    [System.Serializable]
    public struct NoiseSettings
    {
        public int octaves;
        public float lacunarity;
        public float gain;
        public float scale;
    }
    
    public class Chunks
    {
        public int index;
        public Vector3 centerPos;
        public Vector3 startPos;
        private int numPointsPerAxis;
        
        public bool active;
        
        private Vector3[] verticies;
        private int[] triangles;

        private ComputeBuffer pointsBuffer;
        private ComputeBuffer triangleBuffer;

        private InputType input;
        private int numPoints;
        private int numVoxels;
        private int maxTriangleCount;
        
        public Chunks(int index, Vector3 centerPos, Vector3 startPos, InputType input)
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

        public GameObject GetMesh()
        {
            this.Triangulate();
            
            return new GameObject();
        }

        private void Triangulate()
        {
            CreatePointsBuffers();

            ComputeHelper ch = GameObject.FindObjectOfType<ComputeHelper>();
            
            CreateBuffers();
            ch.DispatchNoiseBuffer(pointsBuffer, input.noise, startPos, 8);
            ch.DispatchTriangulateBuffer(pointsBuffer, triangleBuffer, 8);
            
            // copy to arr and set obj
            
            ReleaseBuffers();
        }

        private void CreatePointsBuffers()
        {
            
        }
        
        private void CreateBuffers () {

            pointsBuffer = new ComputeBuffer (numPoints, sizeof (float) * 4);
            triangleBuffer = new ComputeBuffer (maxTriangleCount, sizeof (float) * 3 * 3, ComputeBufferType.Append);
        }

        private void ReleaseBuffers () {
            if (pointsBuffer == null)
            {
                pointsBuffer.Release();
            }
            if (triangleBuffer == null)
            {
                triangleBuffer.Release();
            }
        }
    }
}

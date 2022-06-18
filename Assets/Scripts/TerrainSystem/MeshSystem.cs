using System;
using System.Drawing;
using System.Runtime.ConstrainedExecution;
using UnityEngine;

public class MeshSystem
{
    private ComputeBuffer pointsBuffer;
    private ComputeBuffer fluidBuffer;
    private ComputeBuffer triangleBuffer;
    private ComputeBuffer triangleCountBuffer;
    
    private ComputeShader marchingCubes;
    public ComputeShader MarchingCubes
    {
        set => marchingCubes = value;
    }
    
    public MeshSystem()
    {
        int numPoints = 16 * 16 * 256;
        pointsBuffer = new ComputeBuffer(numPoints, sizeof(int));
        fluidBuffer = new ComputeBuffer(numPoints, sizeof(int));
        triangleBuffer = new ComputeBuffer(numPoints * 5, sizeof (float) * 3 * 3, ComputeBufferType.Append);
        triangleCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
    }

    public (Vector3[], int[]) GenerateMeshData(int[] points)
    {
        triangleBuffer.SetCounterValue(0);

        int kernelMarch = marchingCubes.FindKernel("March");
        pointsBuffer.SetData(points);
        marchingCubes.SetBool("blocky", false);
        marchingCubes.SetBuffer(kernelMarch, "points", pointsBuffer);
        marchingCubes.SetBuffer(kernelMarch, "triangles", triangleBuffer);
        marchingCubes.Dispatch(kernelMarch, 4, 64, 4);
        
        ComputeBuffer.CopyCount(triangleBuffer, triangleCountBuffer, 0);
        int[] triCount = new int[1];
        triangleCountBuffer.GetData(triCount);
        int count = triCount[0];

        Types.Tri[] triangleArray = new Types.Tri[count];
        triangleBuffer.GetData(triangleArray, 0, 0, count);

        Vector3[] verticies = new Vector3[count * 3];
        int[] triangles = new int[count * 3];
        
        for (int i = 0; i < triangleArray.Length; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                int id = 3 * i + j;
                verticies[id] = triangleArray[i][j];
                triangles[id] = id;
            }
        }
        
        return (verticies, triangles);
    }
    
    public (Vector3[], int[]) GenerateFluidData(int[] fluids, int[] points)
    {
        triangleBuffer.SetCounterValue(0);
        
        int kernelFluid = marchingCubes.FindKernel("Fluid");
        pointsBuffer.SetData(points);
        fluidBuffer.SetData(fluids);
        marchingCubes.SetBool("blocky", false);
        marchingCubes.SetBuffer(kernelFluid, "points", pointsBuffer);
        marchingCubes.SetBuffer(kernelFluid, "fluids", fluidBuffer);
        marchingCubes.SetBuffer(kernelFluid, "triangles", triangleBuffer);
        marchingCubes.Dispatch(kernelFluid, 4, 64, 4);
        
        ComputeBuffer.CopyCount(triangleBuffer, triangleCountBuffer, 0);
        int[] triCount = new int[1];
        triangleCountBuffer.GetData(triCount);
        int count = triCount[0];

        Types.Tri[] triangleArray = new Types.Tri[count];
        triangleBuffer.GetData(triangleArray, 0, 0, count);

        Vector3[] verticies = new Vector3[count * 3];
        int[] triangles = new int[count * 3];
        
        for (int i = 0; i < triangleArray.Length; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                int id = 3 * i + j;
                verticies[id] = triangleArray[i][j];
                triangles[id] = id;
            }
        }
        
        return (verticies, triangles);
    }

    public bool SimulateFluidChunks(int[] fluids, int[] points, out Vector3[] verts, out int[] tris)
    {
        // get a count buffer, is the count is 0, return false. Buffers counts the number of change in simulation
        
        // inside shader, simulate water mask for 1 iteration. vertical first, then 8 horizontal directions
        
        // return the new verts and tris, but only make mesh if count buffer is > 0;
        
        throw new NotImplementedException();
    }
    public void Destroy()
    {
        pointsBuffer.Dispose();
        fluidBuffer.Dispose();
        triangleBuffer.Dispose();
        triangleCountBuffer.Dispose();
    }
}

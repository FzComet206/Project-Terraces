using System;
using UnityEngine;

public class MeshSystem
{
    private ComputeBuffer pointsBuffer;
    private ComputeBuffer triangleBuffer;
    private ComputeBuffer triangleCountBuffer;
    
    private ComputeBuffer fluidBuffer;
    private ComputeBuffer simulationCounter;
    
    private ComputeShader marchingCubes;
    public ComputeShader MarchingCubes
    {
        set => marchingCubes = value;
    }
    
    private ComputeShader fluidSim;
    public ComputeShader FluidSim
    {
        set => fluidSim = value;
    }

    public MeshSystem()
    {
        int numPoints = 16 * 16 * 256;
        pointsBuffer = new ComputeBuffer(numPoints, sizeof(int));
        triangleBuffer = new ComputeBuffer(numPoints * 5, sizeof (float) * 3 * 3, ComputeBufferType.Append);
        triangleCountBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        
        fluidBuffer = new ComputeBuffer(numPoints, sizeof(int));
        simulationCounter = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Counter);
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

    public bool SimulateFluidChunks(ref int[] fluids, int[] points)
    {
        // get a count buffer, is the count is 0, return false. Buffers counts the number of change in simulation
        simulationCounter.SetCounterValue(0);
        
        int kernel = fluidSim.FindKernel("FluidSim");
        pointsBuffer.SetData(points);
        fluidBuffer.SetData(fluids);
        marchingCubes.SetBuffer(kernel, "points", pointsBuffer);
        marchingCubes.SetBuffer(kernel, "fluids", fluidBuffer);
        marchingCubes.SetBuffer(kernel, "counter", simulationCounter);
        marchingCubes.Dispatch(kernel, 4, 64, 4);
        
        // get count of the buffer
        int[] _count = new int[1];
        simulationCounter.GetData(_count);
        int count = _count[0];

        if (count == 0)
        {
            return false;
        }
        
        fluidBuffer.GetData(fluids);
        return true;
    }
    public void Destroy()
    {
        pointsBuffer.Dispose();
        fluidBuffer.Dispose();
        triangleBuffer.Dispose();
        
        triangleCountBuffer.Dispose();
        simulationCounter.Dispose();
    }
}

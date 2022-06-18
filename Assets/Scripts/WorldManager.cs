using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class WorldManager : MonoBehaviour
{
    // todo
    // better marching cubes script
    // encode density data to bytes
    // input types for each system

    // inputs
    [SerializeField] DataTypes.NoiseLayerInput noiseInput;
    [SerializeField] DataTypes.ControllerInput controllerInput;
    [SerializeField] DataTypes.ChunkInput chunkInput;
    
    // player ref
    [SerializeField] private GameObject playerRig;
    private PlayerControl player;
    
    // compute shaders
    [SerializeField] private ComputeShader pointsCompute;
    [SerializeField] private ComputeShader marchingCubes;
    [SerializeField] private ComputeShader fluidSim;
    
    // systems
    public ChunkSystem chunkSystem;
    public NoiseSystem noiseSystem;
    public MeshSystem meshSystem;
    public BrushSystem brushSystem;
    public BiomeSystem biomeSystem;
    public StorageSystem storageSystem;

    // properties
    public bool coroutinePause;
    
    // runtimes
    public Queue<int2> simulationQueue;

    // Utils
    private Text fps;

    private void Awake()
    {
        Application.targetFrameRate = 144;
        Screen.SetResolution(2560, 1440, true);
    }

    private void Start()
    {
        // todo make instantiate after init chuks are generated
        Instantiate(playerRig, new Vector3(0, 200, 0), Quaternion.Euler(Vector3.zero));
        
        player = FindObjectOfType<PlayerControl>();
        player.ControllerInput = controllerInput;
        
        InitSystems();
        StartWorld();
        
        fps = FindObjectOfType<Text>();
        StartCoroutine(DisplayFPS());
    }

    private void Update()
    {
        player.ControllerInput = controllerInput;
        chunkSystem.RenderDistance = chunkInput.renderDistance;
    }

    private void InitSystems()
    {
        chunkSystem = new ChunkSystem();
        noiseSystem = new NoiseSystem(noiseInput);
        meshSystem = new MeshSystem();
        brushSystem = new BrushSystem();
        biomeSystem = new BiomeSystem();
        storageSystem = new StorageSystem();

        noiseSystem.PointsCompute = pointsCompute;
        meshSystem.MarchingCubes = marchingCubes;
        meshSystem.FluidSim = fluidSim;

        simulationQueue = new Queue<int2>();
    }

    public void StartWorld()
    {
        StartCoroutine(StartAllCoroutine());
    }

    private IEnumerator StartAllCoroutine()
    {
        // start coroutines in order
        StartCoroutine(WorldGenCoroutine());
        yield return new WaitForFixedUpdate();
        StartCoroutine(ChunkGenCoroutine());
        yield return new WaitForFixedUpdate();
        StartCoroutine(WorldCullCoroutine());
    }

    public IEnumerator WorldGenCoroutine()
    {
        // check nearby chunks
        while (true)
        {
            if (coroutinePause)
            {
                yield break;
            }
            
            chunkSystem.UpdateNearbyChunks(player.transform.position);
            yield return new WaitForSecondsRealtime(0.5f);
        }
    }
    
    public IEnumerator WorldCullCoroutine()
    {
        // delete GetCull
        while (true)
        {
            if (coroutinePause)
            {
                yield break;
            }
            
            // todo have a pool of gameobjects, filters, and coliders for mesh and water
            if (chunkSystem.generated.Count > chunkInput.maxChunksBeforeCull)
            {
                int2 coord = chunkSystem.GetCull(player.transform.position);
                ChunkMemory chunkMemory = chunkSystem.chunksDict[coord];
                
                chunkSystem.chunksDict.Remove(coord);
                chunkSystem.generated.Remove(coord);
                
                chunkMemory.chunk.Active = false;
                chunkMemory.chunk.data = null;

                Destroy(chunkMemory.meshChunk.GetComponent<MeshFilter>().sharedMesh);
                Destroy(chunkMemory.meshChunk.GetComponent<MeshCollider>().sharedMesh);
                Destroy(chunkMemory.meshChunk);
                
                Destroy(chunkMemory.waterChunk.GetComponent<MeshFilter>().sharedMesh);
                Destroy(chunkMemory.waterChunk);
            }
            
            yield return new WaitForEndOfFrame();
        }
    }
    
    public IEnumerator ChunkGenCoroutine()
    {
        // generate chunks
        while (true)
        {
            if (coroutinePause)
            {
                yield break;
            }
            
            if (!player.updating)
            {
                int c = chunkSystem.queue.Count;
                if (c >= chunkInput.chunksPerFrame)
                {
                    for (int i = 0; i < chunkInput.chunksPerFrame; i++)
                    {
                        GetNewChunk();
                    }
                }
                else
                {
                    for (int i = 0; i < c; i++)
                    {
                        GetNewChunk();
                    }
                }
            }
            yield return new WaitForEndOfFrame();
        }
    }

    private IEnumerator FluidCoroutine()
    {
        // simulate fluid mesh for nearby 25 chunks, every 0.5 seconds
        // when chunk is removed from queue and player is not with radius, dont simulate
    
        while (true)
        {
            chunkSystem.GetNearbyChunks(player.transform.position, ref simulationQueue);
            while (simulationQueue.Count > 0)
            {
                int2 coord = simulationQueue.Dequeue();
                
                yield return new WaitForEndOfFrame();
            }

            yield return new WaitForSeconds(0.5f);
        }
    }
    
    // ReSharper disable Unity.PerformanceAnalysis
    private void GetNewChunk()
    {
        Chunk chunk = chunkSystem.queue.Dequeue();
        int[] points;
        int[] fluids;
        (points, fluids) = noiseSystem.DispatchPointBuffer(chunk);
        (Vector3[] verts, int[] tris) = meshSystem.GenerateMeshData(points);
        (Vector3[] vertsfluid, int[] trisfluid) = meshSystem.GenerateFluidData(fluids, points);

        chunk.data = points;
        chunk.fluid = fluids;
        chunk.Active = true;
        chunk.Generated = true;

        // mesh generation
        Mesh mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        GameObject chunkObj = new GameObject("chunk " + chunk.coordX + " " + chunk.coordZ,
            typeof(MeshFilter), typeof(MeshCollider), typeof(MeshRenderer));
        chunkObj.transform.parent = chunkInput.meshParent;
        chunkObj.transform.position = new Vector3(chunk.startPositionX, 0, chunk.startPositionZ);

        MeshFilter meshFilter = chunkObj.GetComponent<MeshFilter>();
        MeshCollider meshCollider = chunkObj.GetComponent<MeshCollider>();
        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;
        chunkObj.GetComponent<MeshRenderer>().sharedMaterial = chunkInput.meshMaterial;
        
        // fluid generation
        Mesh fluidMesh = new Mesh();
        fluidMesh.SetVertices(vertsfluid);
        fluidMesh.SetTriangles(trisfluid, 0);
        fluidMesh.RecalculateNormals();

        GameObject fluidObj = new GameObject("fluidChunk " + chunk.coordX + " " + chunk.coordZ,
            typeof(MeshFilter), typeof(MeshRenderer));
        fluidObj.transform.parent = chunkInput.meshParent;
        fluidObj.transform.position = new Vector3(chunk.startPositionX, 0, chunk.startPositionZ);
        
        MeshFilter meshFilterFluid = fluidObj.GetComponent<MeshFilter>();
        meshFilterFluid.sharedMesh = fluidMesh;
        fluidObj.GetComponent<MeshRenderer>().sharedMaterial = chunkInput.fluidMaterial;
        
        // update ds
        int2 coord = new int2(chunk.coordX, chunk.coordZ);
        chunkSystem.generated.Add(coord);
        chunkSystem.inQueue.Remove(coord);
        chunkSystem.chunksDict[coord] = new ChunkMemory(chunkObj, fluidObj, chunk);
    }
    
    IEnumerator DisplayFPS()
    {
        while (true)
        {
            fps.text = String.Format("{0} FPS", Mathf.RoundToInt(1f / Time.deltaTime));
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void OnDestroy()
    {
        noiseSystem.Destroy();
        meshSystem.Destroy();
    }
}

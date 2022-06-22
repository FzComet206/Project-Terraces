using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
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
    public FluidSystem fluidSystem;
    public BiomeSystem biomeSystem;
    public StorageSystem storageSystem;

    // properties
    public bool coroutinePause;
    
    // runtimes
    public Queue<int2> simulationQueue;

    // Utils
    private Text fps;
    private Text threadSpeed;

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
        
        fps = FindObjectsOfType<Text>()[1];
        threadSpeed = FindObjectsOfType<Text>()[0];
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
        
        fluidSystem = new FluidSystem(chunkSystem);

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
        yield return new WaitForSecondsRealtime(3f);
        StartCoroutine(FluidCoroutine());
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
                Chunk chunk = chunkSystem.chunksDict[coord];
                
                chunkSystem.chunksDict.Remove(coord);
                chunkSystem.generated.Remove(coord);
                
                chunk.Active = false;
                chunk.data = null;

                Destroy(chunk.MeshFil.sharedMesh);
                Destroy(chunk.MeshCol.sharedMesh);
                Destroy(chunk.MeshObj);

                Destroy(chunk.FluidFil.sharedMesh);
                Destroy(chunk.FluidObj);
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
        while (true)
        {
            // start thread
            fluidSystem.playerPos = player.transform.position;
            Thread last = ThreadManager.Worker(fluidSystem);
            
            // get update coord
            int2 origin = new int2((int)(player.transform.position.x / 15f), (int)(player.transform.position.z / 15f));
            Queue<int2> process = new Queue<int2>();
            for (int i = -3; i < 4; i++)
            {
                for (int j = -3; j < 4 ; j++)
                {
                    process.Enqueue(new int2(i, j) + origin);
                }
            }
            
            // wait
            yield return new WaitForSecondsRealtime(0.5f);

            if (last.IsAlive)
            {
                Debug.Log("aborting thread");
                last.Abort();
            }

            // chain update
            StartCoroutine(FluidUpdate(fluidSystem.update));
        }
    }

    private IEnumerator FluidUpdate(List<int2> process)
    {
        List<int2> update = new List<int2>();
        foreach (var v in process)
        {
            update.Add(v);
        }

        int c = 0;
        foreach (var coord in update)
        {
            Chunk ck = chunkSystem.chunksDict[coord];
            GameObject chunkObject = ck.FluidObj;

            (Vector3[] verts, int[] tris) = meshSystem.GenerateFluidData(ck.fluid, ck.data);

            ck.FluidFil.sharedMesh.Clear();
            ck.FluidFil.sharedMesh.SetVertices(verts);
            ck.FluidFil.sharedMesh.SetTriangles(tris, 0);
            ck.FluidFil.sharedMesh.RecalculateNormals();
            ck.FluidFil.sharedMesh.RecalculateTangents();
            c++;
            if (c % 3 == 0)
            {
                yield return new WaitForEndOfFrame();
            }
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
        mesh.indexFormat = IndexFormat.UInt32;
        mesh.MarkDynamic();
        
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();

        GameObject chunkObj = new GameObject("chunk " + chunk.coordX + " " + chunk.coordZ,
            typeof(MeshFilter), typeof(MeshCollider), typeof(MeshRenderer));
        chunkObj.transform.parent = chunkInput.meshParent;
        chunkObj.transform.position = new Vector3(chunk.startPositionX, 0, chunk.startPositionZ);
        MeshFilter meshFilter = chunkObj.GetComponent<MeshFilter>();
        MeshCollider meshCollider = chunkObj.GetComponent<MeshCollider>();
        chunkObj.GetComponent<MeshRenderer>().sharedMaterial = chunkInput.meshMaterial;
        
        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;

        
        
        // fluid generation
        Mesh fluidMesh = new Mesh();
        fluidMesh.indexFormat = IndexFormat.UInt32;
        fluidMesh.MarkDynamic();
        fluidMesh.SetVertices(vertsfluid);
        fluidMesh.SetTriangles(trisfluid, 0);
        fluidMesh.RecalculateNormals();
        fluidMesh.RecalculateTangents();

        GameObject fluidObj = new GameObject("fluidChunk " + chunk.coordX + " " + chunk.coordZ,
            typeof(MeshFilter), typeof(MeshRenderer));
        fluidObj.transform.parent = chunkInput.meshParent;
        fluidObj.transform.position = new Vector3(chunk.startPositionX, 0, chunk.startPositionZ);
        
        MeshFilter meshFilterFluid = fluidObj.GetComponent<MeshFilter>();
        meshFilterFluid.sharedMesh = fluidMesh;
        fluidObj.GetComponent<MeshRenderer>().sharedMaterial = chunkInput.fluidMaterial;
        
        // update ds
        int2 coord = new int2(chunk.coordX, chunk.coordZ);
        
        chunk.MeshObj = chunkObj;
        chunk.FluidObj = fluidObj;
        chunk.MeshFil = meshFilter;
        chunk.FluidFil = meshFilterFluid;
        chunk.MeshCol = meshCollider;
        
        chunkSystem.generated.Add(coord);
        chunkSystem.inQueue.Remove(coord);
        chunkSystem.chunksDict[coord] = chunk;
    }
    
    IEnumerator DisplayFPS()
    {
        while (true)
        {
            fps.text = String.Format("{0} FPS", Mathf.RoundToInt(1f / Time.deltaTime));
            threadSpeed.text = String.Format("TS {0} ms", fluidSystem.threadSpeed);
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void OnDestroy()
    {
        noiseSystem.Destroy();
        meshSystem.Destroy();
    }
}

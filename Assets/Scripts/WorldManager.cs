using System;
using System.Collections;
using Unity.Mathematics;
using UnityEngine;
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
    
    // systems
    private ChunkSystem chunkSystem;
    private NoiseSystem noiseSystem;
    private MeshSystem meshSystem;
    private FluidSystem fluidSystem;
    private BrushSystem brushSystem;
    private BiomeSystem biomeSystem;
    private StorageSystem storageSystem;
    
    // Utils
    private Text fps;

    private void Awake()
    {
        Application.targetFrameRate = 144;
        Screen.SetResolution(1920, 1080, false);
    }

    private void Start()
    {
        Instantiate(playerRig, Vector3.zero, Quaternion.Euler(Vector3.zero));
        player = FindObjectOfType<PlayerControl>();
        player.ControllerInput = controllerInput;
        InitSystems();
        StartWorld();
        
        fps = FindObjectOfType<Text>();
        StartCoroutine(DisplayFPS());

        // init chunks array and properties
        // update player position to chunks
        // chunk output chunk indexes and parameters
        // noise and mesh system triangulation
        // start chunks coroutines triangulation 
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
        fluidSystem = new FluidSystem();
        
        brushSystem = new BrushSystem();
        biomeSystem = new BiomeSystem();
        storageSystem = new StorageSystem();

        noiseSystem.PointsCompute = pointsCompute;
        meshSystem.MarchingCubes = marchingCubes;
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
    }

    private IEnumerator WorldGenCoroutine()
    {
        // check nearby chunks
        while (true)
        {
            chunkSystem.UpdateNearbyChunks(player.transform.position);
            yield return new WaitForSecondsRealtime(0.5f);
        }
    }
    
    private IEnumerator ChunkGenCoroutine()
    {
        // generate chunks
        while (true)
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
            yield return new WaitForEndOfFrame();
        }
    }

    private IEnumerator WorldCullCoroutine()
    {
        // delete GetCull
        throw new NotImplementedException();
    }

    private IEnumerator BrushCoroutine()
    {
        // input cursor position if held or if clicked
        throw new NotImplementedException();
    }
    
    private IEnumerator FluidCoroutine()
    {
        throw new NotImplementedException();
    }
    
    private void GetNewChunk()
    {
        Chunk c = chunkSystem.queue.Dequeue();
        int[] points = noiseSystem.DispatchPointBuffer(c);
        (Vector3[] verts, int[] tris) = meshSystem.GenerateMeshData(points);

        Mesh mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        GameObject chunk = new GameObject("chunk " + c.coordX + " " + c.coordZ,
            typeof(MeshFilter), typeof(MeshCollider), typeof(MeshRenderer));
        chunk.transform.parent = chunkInput.meshParent;
        chunk.transform.position = chunk.transform.position + new Vector3(c.startPositionX, 0, c.startPositionZ);

        MeshFilter meshFilter = chunk.GetComponent<MeshFilter>();
        MeshCollider meshCollider = chunk.GetComponent<MeshCollider>();
        meshFilter.sharedMesh = mesh;
        meshCollider.sharedMesh = mesh;
        chunk.GetComponent<MeshRenderer>().material = chunkInput.meshMaterial;

        int2 coord = new int2(c.coordX, c.coordZ);
        chunkSystem.generated.Add(coord);
        chunkSystem.inQueue.Remove(coord);
        chunkSystem.chunksDict[coord] = chunk;
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

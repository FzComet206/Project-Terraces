using System;
using System.Collections;
using UnityEngine;

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
    
    // systems
    private ChunkSystem chunkSystem;
    private NoiseSystem noiseSystem;
    private MeshSystem meshSystem;
    private FluidSystem fluidSystem;
    private BrushSystem brushSystem;
    private BiomeSystem biomeSystem;
    private StorageSystem storageSystem;

    private void Awake()
    {
        Application.targetFrameRate = 300;
        Screen.SetResolution(1080, 1440, false);
    }

    private void Start()
    {
        Instantiate(playerRig, Vector3.zero, Quaternion.Euler(Vector3.zero));
        player = FindObjectOfType<PlayerControl>();
        player.ControllerInput = controllerInput;
        InitSystems();
        StartWorld();

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
        StartCoroutine(WorldCullCoroutine());
        yield return new WaitForFixedUpdate();
        StartCoroutine(BrushCoroutine());
        yield return new WaitForFixedUpdate();
        StartCoroutine(FluidCoroutine());
        yield return null;
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
            for (int i = 0; i < chunkInput.chunksPerFrame; i++)
            {
                Chunk c = chunkSystem.queue.Dequeue();
                // generate points
                // generate vert and tris
                // generate gameobjects
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
}

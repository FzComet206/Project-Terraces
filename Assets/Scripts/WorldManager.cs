using UnityEngine;

public class WorldManager : MonoBehaviour
{
    // todo
    // better marching cubes script
    // encode density data to bytes
    // input types for each system

    // inputs
    [SerializeField] DataTypes.NoiseLayerInput noiseLayerOneInput;
    [SerializeField] DataTypes.NoiseLayerInput noiseLayerTwoInput;
    [SerializeField] DataTypes.ControllerInput controllerInput;
    [SerializeField] DataTypes.ChunkInput chunkInput;
    [SerializeField] DataTypes.MeshInput meshInput;
    [SerializeField] DataTypes.FluidInput fluidInput;
    
    // player ref
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
        player = FindObjectOfType<PlayerControl>();
        InitSystems();
        
        // init chunks array and properties
        // update player position to chunks
        // chunk output chunk indexes and parameters
        // noise and mesh system triangulation
        // start chunks coroutines triangulation 
    }

    private void InitSystems()
    {
        chunkSystem = new ChunkSystem(chunkInput);
        noiseSystem = new NoiseSystem(noiseLayerOneInput, noiseLayerTwoInput);
        meshSystem = new MeshSystem();
        fluidSystem = new FluidSystem();
        brushSystem = new BrushSystem();
        biomeSystem = new BiomeSystem();
        storageSystem = new StorageSystem();
    }
}

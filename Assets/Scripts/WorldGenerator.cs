using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    [SerializeField] private Types.InputType mapInput;
    
    private Controller playerRef;
    private Chunks[] chunks;
    private Queue<Chunks> queue;
    private Queue<GameObject> testQueue;

    private void Start()
    {
        int d = mapInput.mapDivision; 
        playerRef = FindObjectOfType<Controller>();
        chunks = new Chunks[d * d * d];
        queue = new Queue<Chunks>();
        testQueue = new Queue<GameObject>();
        InitBoundingBox();
        GenerateAllChunksConfig();
        StartCoroutine(ChunkUpdate());
        StartCoroutine(MeshUpdate());
    }

    private IEnumerator ChunkUpdate()
    {
        while (true)
        {
            UpdateChunks();
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator MeshUpdate()
    {
        while (true)
        {
            if (testQueue.Count != 0)
            {
                GameObject c = testQueue.Dequeue();
                c.SetActive(true);
            }
            yield return new WaitForFixedUpdate();
        }
    }

    void GenerateAllChunksConfig()
    {
        
    }

    void UpdateChunks()
    {
        
    }

    public void InitBoundingBox()
    {
        Vector3[] dirs = new Vector3[]
        {
            Vector3.up,
            Vector3.down,
            Vector3.left,
            Vector3.right,
            Vector3.forward,
            Vector3.back
        };

        foreach (Vector3 d in dirs)
        {
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Plane);
            p.transform.position = d * mapInput.mapSize;
            p.transform.rotation = Quaternion.LookRotation(
                new Vector3(d.z, d.x, d.y),
                -d
            );
            p.transform.localScale = Vector3.one * mapInput.mapSize / 5;
            p.GetComponent<MeshRenderer>().material = mapInput.boundMat;
        }
    }
    
    
    
    
    
    
    
}

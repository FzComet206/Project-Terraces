using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    [SerializeField] private Types.InputType mapInput;
    
    private Controller playerRef;
    private Chunks[] chunks;
    private Queue<Chunks> queue;

    private Vector3[] testBlock;
    private Queue<Vector3> testQueue;
    private HashSet<int> generated;

    private void Start()
    {
        int d = mapInput.mapDivision; 
        playerRef = FindObjectOfType<Controller>();
        chunks = new Chunks[d * d * d];
        queue = new Queue<Chunks>();
        
        testQueue = new Queue<Vector3>();
        testBlock = new Vector3[d * d * d];
        generated = new HashSet<int>();
        
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
                GameObject g = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Vector3 p = testQueue.Dequeue();
                g.GetComponent<Transform>().position = p;
            }
            yield return new WaitForFixedUpdate();
        }
    }

    void GenerateAllChunksConfig()
    {
        int d = mapInput.mapDivision;
        int s = mapInput.pointsPerChunkAxis;
        for (int i = 0; i < d; i++)
        {
            for (int j = 0; j < d; j++)
            {
                for (int k = 0; k < d; k++)
                {
                    float x = i - d / 2; 
                    float y = j - d / 2; 
                    float z = k - d / 2;
                    Vector3 p = new Vector3(x, y, z) * s;
                    int index = k + j * d + i * d * d;
                    testBlock[index] = p;
                }
            }
        }
    }

    void UpdateChunks()
    {
        int d = mapInput.mapDivision;
        int s = mapInput.pointsPerChunkAxis;
        Vector3 p = playerRef.transform.position;

        int x = Mathf.RoundToInt(p.x / s + (d / 2f));
        int y = Mathf.RoundToInt(p.y / s + (d / 2f));
        int z = Mathf.RoundToInt(p.z / s + (d / 2f));
        
        for (int i = -2; i < 3; i++)
        {
            for (int j = -2; j < 3; j++)
            {
                for (int k = -2; k < 3; k++)
                {
                    int _i = x + i;
                    int _j = y + j;
                    int _k = z + k;
                    
                    int index = _k + _j * d + _i * d * d;

                    if (index < testBlock.Length && index >= 0 && !generated.Contains(index))
                    {
                        testQueue.Enqueue(testBlock[index]);
                        generated.Add(index);
                    }
                }
            }
        }
    }
}

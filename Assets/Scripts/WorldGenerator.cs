using System.Collections;
using Unity.Mathematics;
using UnityEngine;

public class WorldGenerator : MonoBehaviour
{
    [SerializeField] private Types.InputType mapInput;
    
    private Controller playerRef;
    private Types.Chunks[] chunks;

    private void Start()
    {
        playerRef = FindObjectOfType<Controller>();
        chunks = new Types.Chunks[(int)Mathf.Pow(mapInput.mapDivision * 2 - 2, 3)];
        InitBoundingBox();
        GetAllChunks();
        StartCoroutine(ChunkUpdate());
    }

    public IEnumerator ChunkUpdate()
    {
        while (true)
        {
            DetectAndUpdateChunks();
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void DetectAndUpdateChunks()
    {
        int stepSize = mapInput.mapSize/ mapInput.mapDivision;
        int axisNum = mapInput.mapDivision * 2 - 2;

        float offset = -30f;
        
        // convert position to index
        Vector3 pos = playerRef.transform.position;
        int x = Mathf.RoundToInt((pos.x + offset) / stepSize + mapInput.mapDivision);
        int y = Mathf.RoundToInt((pos.y + offset) / stepSize + mapInput.mapDivision);
        int z = Mathf.RoundToInt((pos.z + offset) / stepSize + mapInput.mapDivision);

        for (int i = -1; i < 2; i++)
        {
            for (int j = -1; j < 2; j++)
            {
                for (int k = -1; k < 2; k++)
                {
                    int _x = x + i;
                    int _y = y + j;
                    int _z = z + k;
                    
                    int index = _z + _y * axisNum + _x * axisNum * axisNum;
                    
                    if (index < chunks.Length && index >= 0)
                    {
                        Types.Chunks c = chunks[index];
                        c.active = true;
                        c.Refresh();
                    }
                }
            }
        }
    }

    public void GetAllChunks()
    {
        int size = mapInput.mapSize;
        int div = mapInput.mapDivision;
        int stepSize = size / div;
        int axisNum = div * 2 - 2;

        for (int i = 0; i < axisNum; i++)
        {
            for (int j = 0; j < axisNum; j++)
            {
                for (int k = 0; k < axisNum; k++)
                {
                    int x = (i - div + 1) * stepSize;
                    int y = (j - div + 1) * stepSize;
                    int z = (k - div + 1) * stepSize;

                    int3 startPos = new int3(x, y, z);
                    Vector3 centerPos = new Vector3(x + stepSize / 2, y + stepSize / 2, z + stepSize / 2);
                    
                    int index = k +
                                j * axisNum +
                                i * axisNum * axisNum;
                    
                    chunks[index] = new Types.Chunks(index, centerPos, startPos, mapInput);
                }
            }
        }
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

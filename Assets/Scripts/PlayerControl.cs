using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControl : MonoBehaviour
{
    // manage input
    // manage raycast, movement, positions
    // communicate with world manager
    private DataTypes.ControllerInput controllerInput;

    public DataTypes.ControllerInput ControllerInput
    {
        get => controllerInput;
        set => controllerInput = value;
    }
    
    [SerializeField] private InputAction modify;
    [SerializeField] private InputAction adding;
    [SerializeField] private InputAction subtracting;
    
    [SerializeField] private InputAction cruise;
    [SerializeField] private InputAction mouse;
    
    [SerializeField] private InputAction quit;
    
    public bool deleted = false;

    [SerializeField] float cruiseSpeed;
    [SerializeField] float rotateSpeed;
    [SerializeField] GameObject cursorHead;

    private Rigidbody rb;
    private Camera cam;
    private Generator generator;

    private float xRotation = 0f;
    private float yRotation = 0f;

    private Vector3 cursorPosition;
    private bool mod = false;

    private WorldManager worldManager;

    public bool updating;

    private void Start()
    {
        worldManager = FindObjectOfType<WorldManager>();
        rb = gameObject.GetComponent<Rigidbody>();
        cam = GetComponentInChildren<Camera>();
        generator = FindObjectOfType<Generator>();
        
        Cursor.lockState = CursorLockMode.Locked;
        
        StartCoroutine(BrushCoroutine());
        StartCoroutine(MoveAndRotate());
    }

    void Update()
    {
        // raycast to point
        float midX = Screen.width / 2f;
        float midY = Screen.height / 2f;
        Vector3 screenPoint = new Vector3(midX, midY, 1);
        Ray cursorRay = cam.ScreenPointToRay(screenPoint);
        RaycastHit hit = new RaycastHit();
        Physics.Raycast(cursorRay, out hit, 300f);
        cursorPosition = hit.point + Vector3.ClampMagnitude(hit.normal, 0.2f);
        
        // determine is modifying
        float m = modify.ReadValue<float>();
        if (m > 0.01f)
        {
            mod = !mod;
            modify.Disable();
            Invoke("SetModTimer", 0.3f);
        }

        // cursor
        if (mod)
        {
            cursorHead.SetActive(true);
            cursorHead.transform.position = cursorPosition;
        }
        else
        {
            cursorHead.SetActive(false);
        }

        if (quit.ReadValue<float>() > 0f)
        {
            Debug.Log("application quitting");
            // save data then quit
            Application.Quit();
        }
    }

    IEnumerator MoveAndRotate()
    {
        while (true)
        {
            // movement
            Vector3 c = cruise.ReadValue<Vector3>();
            Vector3 forward = c.z * transform.forward;
            Vector3 right = c.x * transform.right;
            Vector3 up = c.y * Vector3.up;
            Vector3 vel = (forward + up + right).normalized * (cruiseSpeed * Time.fixedDeltaTime);
            rb.velocity = vel;
            
            // rotation
            Vector2 delta = mouse.ReadValue<Vector2>();
            float rx = delta.y * rotateSpeed * Time.deltaTime;
            float ry = delta.x * rotateSpeed * Time.deltaTime;
            xRotation -= rx;
            xRotation = Mathf.Clamp(xRotation, -90, 90);
            yRotation += ry;
            transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f).normalized;
            yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator BrushCoroutine()
    {
        while (true)
        {
            if (mod)
            {
                if (adding.ReadValue<float>() > 0f)
                {
                    updating = true;
                    ProcessOps(true);
                } else if (subtracting.ReadValue<float>() > 0f)
                {
                    updating = true;
                    ProcessOps(false);
                }
                else
                {
                    updating = false;
                }
            }
            yield return new WaitForSecondsRealtime(0.02f);
        }
    }

    private void ProcessOps(bool add)
    {
        List<VoxelOperation> ops = worldManager.brushSystem.EvaluateBrush(cursorPosition);
        HashSet<int2> coords = new HashSet<int2>();
        for (int i = 0; i < ops.Count; i++)
        {
            VoxelOperation voxelOperation = ops[i];
            
            int2 coord = voxelOperation.coord;
            if (!coords.Contains(coord))
            {
                coords.Add(coord);
            }

            int localIndex = voxelOperation.localIndex;
            int op = voxelOperation.densityOperation;
            OperationType opType = voxelOperation.opType;

            ChunkMemory chunkMemory;
            bool contains = worldManager.chunkSystem.chunksDict.TryGetValue(coord, out chunkMemory);

            if (!contains)
            {
                continue;
            }

            Chunk chunk = chunkMemory.chunk;

            switch (opType)
            {
                case OperationType.add:
                    if (add)
                    {
                        chunk.data[localIndex] += op;
                    }
                    else
                    {
                        chunk.data[localIndex] -= op;
                    }
                    break;
                case OperationType.set:
                    
                    if (add)
                    {
                        if (op > 0)
                        {
                            chunk.data[localIndex] = op;
                        }
                    }
                    else
                    {
                        if (op > 0)
                        {
                            chunk.data[localIndex] = -op;
                        }
                    }
                    break;
                case OperationType.special:

                    int diff = Math.Abs(Math.Clamp(chunk.data[localIndex], -32, 31));
                    chunk.data[localIndex] -= diff;
                    break;
            }
        }

        foreach (var coord in coords)
        {
            ChunkMemory chunkMemory;
            bool contains = worldManager.chunkSystem.chunksDict.TryGetValue(coord, out chunkMemory);
            
            if (!contains)
            {
                continue;
            }

            GameObject chunkObject = chunkMemory.gameObject;
            Chunk chunk = chunkMemory.chunk;
            
            (Vector3[] verts, int[] tris) = worldManager.meshSystem.GenerateMeshData(chunk.data);
            MeshFilter mf = chunkObject.GetComponent<MeshFilter>();
            MeshCollider mc = chunkObject.GetComponent<MeshCollider>();

            mf.sharedMesh.Clear();
            mf.sharedMesh.SetVertices(verts);
            mf.sharedMesh.SetTriangles(tris, 0);
            mf.sharedMesh.RecalculateNormals();
            mf.sharedMesh.RecalculateBounds();

            mc.sharedMesh = null;
            mc.sharedMesh = mf.sharedMesh;
        }
    }

    IEnumerator SetModTimer()
    {
        modify.Enable();
        return null;
    }
    
    private void OnEnable()
    {
        cruise.Enable();
        mouse.Enable();
        modify.Enable();
        adding.Enable();
        subtracting.Enable();
        quit.Enable();
    }

    private void OnDisable()
    {
        cruise.Disable();
        mouse.Disable();
        modify.Disable();
        adding.Disable();
        subtracting.Disable();
        quit.Disable();
    }
}

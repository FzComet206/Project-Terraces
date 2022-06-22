using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;

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

    [SerializeField] private InputAction switchBrush;
    [SerializeField] private InputAction mouseScrollDelta;
    
    [SerializeField] float cruiseSpeed;
    [SerializeField] float rotateSpeed;
    [SerializeField] GameObject cursorHead;

    [SerializeField] private Material addBrush;
    [SerializeField] private Material waterBrush;
    [SerializeField] private Material specialBrush;

    private Rigidbody rb;
    private Camera cam;
    private Generator generator;

    private float xRotation = 0f;
    private float yRotation = 0f;

    private Vector3 cursorPosition;
    public bool mod = false;

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
        StartCoroutine(FixedInputUpdate());
        StartCoroutine(InputCapture());
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

    IEnumerator InputCapture()
    {
        BrushSystem brushSystem = worldManager.brushSystem;
        while (true)
        {
            if (switchBrush.ReadValue<float>() > 0)
            {
                if (brushSystem.opType == OperationType.add)
                {
                    brushSystem.opType = OperationType.water;
                    cursorHead.GetComponent<MeshRenderer>().material = waterBrush;
                } else if (brushSystem.opType == OperationType.water)
                {
                    brushSystem.opType = OperationType.special;
                    cursorHead.GetComponent<MeshRenderer>().material = specialBrush;
                }
                else
                {
                    brushSystem.opType = OperationType.add;
                    cursorHead.GetComponent<MeshRenderer>().material = addBrush;
                }

                // cooldown
            }


            yield return new WaitForSecondsRealtime(0.1f);
        }
    }

    IEnumerator FixedInputUpdate()
    {
        while (true)
        {
            // brush size
            cursorHead.transform.localScale = Vector3.one * worldManager.brushSystem.brushSize / 1.5f;
            Vector2 deltaScroll = mouseScrollDelta.ReadValue<Vector2>();
            float sx = deltaScroll.y * 1f * Time.fixedDeltaTime;
            float sy = deltaScroll.x * 1f * Time.fixedDeltaTime;
            float size = (float) worldManager.brushSystem.brushSize;

            size += sx;
            size -= sy;
            int fsize = Mathf.Clamp(Mathf.RoundToInt(size), 2, 10);
            worldManager.brushSystem.brushSize = fsize;
            
            // movement
            Vector3 c = cruise.ReadValue<Vector3>();
            Vector3 forward = c.z * transform.forward;
            Vector3 right = c.x * transform.right;
            Vector3 up = c.y * Vector3.up;
            Vector3 vel = (forward + up + right).normalized * (cruiseSpeed * Time.fixedDeltaTime);
            rb.velocity = vel;
            
            // rotation
            Vector2 delta = mouse.ReadValue<Vector2>();
            float rx = delta.y * rotateSpeed * Time.fixedDeltaTime;
            float ry = delta.x * rotateSpeed * Time.fixedDeltaTime;
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

            Chunk chunk;
            bool contains = worldManager.chunkSystem.chunksDict.TryGetValue(coord, out chunk);

            if (!contains)
            {
                continue;
            }

            switch (opType)
            {
                case OperationType.add:
                    if (add)
                    {
                        chunk.data[localIndex] += op;
                        if (chunk.data[localIndex] > 0)
                        {
                            chunk.fluid[localIndex] = 0;
                        }
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
                            chunk.fluid[localIndex] = 0;
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
                
                case OperationType.water:
                    if (add)
                    {
                        chunk.fluid[localIndex] = 2;
                    }
                    else
                    {
                        chunk.fluid[localIndex] = 0;
                    }
                    break;
            }
        }

        if (ops[0].opType == OperationType.water)
        {
            ProcessFluidOp(coords);
            return;
        }

        ProcessFluidOp(coords);
        foreach (var coord in coords)
        {
            Chunk chunk;
            bool contains = worldManager.chunkSystem.chunksDict.TryGetValue(coord, out chunk);
            
            if (!contains)
            {
                continue;
            }

            (Vector3[] verts, int[] tris) = worldManager.meshSystem.GenerateMeshData(chunk.data);
            MeshFilter mf = chunk.MeshFil;
            MeshCollider mc = chunk.MeshCol;

            mf.sharedMesh.Clear();
            mf.sharedMesh.SetVertices(verts);
            mf.sharedMesh.SetTriangles(tris, 0);
            mf.sharedMesh.RecalculateNormals();
            mf.sharedMesh.RecalculateBounds();
            mf.sharedMesh.RecalculateTangents();

            mc.sharedMesh = null;
            mc.sharedMesh = mf.sharedMesh;
        }
    }

    private void ProcessFluidOp(HashSet<int2> coords)
    {
        foreach (var coord in coords)
        {
            Chunk chunk;
            bool contains = worldManager.chunkSystem.chunksDict.TryGetValue(coord, out chunk);

            if (!contains)
            {
                continue;
            }

            (Vector3[] verts, int[] tris) = worldManager.meshSystem.GenerateFluidData(chunk.fluid, chunk.data);
            MeshFilter mf = chunk.FluidFil;

            mf.sharedMesh.Clear();
            mf.sharedMesh.SetVertices(verts);
            mf.sharedMesh.SetTriangles(tris, 0);
            mf.sharedMesh.RecalculateNormals();
            mf.sharedMesh.RecalculateBounds();
            mf.sharedMesh.RecalculateTangents();
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
        switchBrush.Enable();
        mouseScrollDelta.Enable();
        quit.Enable();
    }

    private void OnDisable()
    {
        cruise.Disable();
        mouse.Disable();
        modify.Disable();
        adding.Disable();
        subtracting.Disable();
        switchBrush.Disable();
        mouseScrollDelta.Disable();
        quit.Disable();
    }
}

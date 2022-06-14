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
        cam = Camera.main;
        generator = FindObjectOfType<Generator>();
        Cursor.lockState = CursorLockMode.Locked;
        StartCoroutine(BrushCoroutine());
    }


    void FixedUpdate()
    {
        // cruise
        Vector3 c = cruise.ReadValue<Vector3>();
        Vector3 forward = c.z * transform.forward;
        Vector3 right = c.x * transform.right;
        Vector3 up = c.y * Vector3.up;

        Vector3 vel = (forward + up + right).normalized * (cruiseSpeed * Time.fixedDeltaTime);
        
        rb.velocity = vel;
        // rotate
        Vector2 delta = mouse.ReadValue<Vector2>();
        float rx = delta.y * rotateSpeed * Time.fixedDeltaTime;
        float ry = delta.x * rotateSpeed * Time.fixedDeltaTime;

        xRotation -= rx;
        xRotation = Mathf.Clamp(xRotation, -90, 90);
        yRotation += ry;

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f).normalized;
        
        // raycast to point
        float midX = Screen.width / 2f;
        float midY = Screen.height / 2f;
        Vector3 screenPoint = new Vector3(midX, midY, 1);
        Ray cursorRay = cam.ScreenPointToRay(screenPoint);
        RaycastHit hit = new RaycastHit();
        Physics.Raycast(cursorRay, out hit, 300f);
        cursorPosition = hit.point + Vector3.ClampMagnitude(hit.normal, 0.3f);
        
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
            Application.Quit();
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
            yield return new WaitForSecondsRealtime(0.05f);
        }
    }

    private void ProcessOps(bool add)
    {
        List<BrushSystem.VoxelOperation> ops = worldManager.brushSystem.EvaluateBrush(cursorPosition);
        HashSet<int2> coords = new HashSet<int2>();
        for (int i = 0; i < ops.Count; i++)
        {
            int2 coord = ops[i].coord;
            if (!coords.Contains(coord))
            {
                coords.Add(coord);
            }

            int localIndex = ops[i].localIndex;
            int op = ops[i].densityOperation;

            try
            {
                (_, Chunk chunk) = worldManager.chunkSystem.chunksDict[coord];
                if (add)
                {
                    chunk.data[localIndex] = op;
                }
                else
                {
                    chunk.data[localIndex] = -op;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        foreach (var coord in coords)
        {
            (GameObject chunkObject, Chunk chunk) = worldManager.chunkSystem.chunksDict[coord];
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

    IEnumerator TestCoord(GameObject go)
    {
        Vector3 b = go.transform.position;
        Vector3 p = go.transform.position;
        p.y += 5;
        go.transform.position = p;
        yield return new WaitForSecondsRealtime(0.1f);
        go.transform.position = b;
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

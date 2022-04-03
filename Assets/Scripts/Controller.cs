using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Controller : MonoBehaviour
{
    [SerializeField] private InputAction modify;
    [SerializeField] private InputAction adding;
    [SerializeField] private InputAction subtracting;
    [SerializeField] private InputAction quit;
    [SerializeField] private InputAction blocky;
    
    [SerializeField] private InputAction cruise;
    [SerializeField] private InputAction mouse;

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
    
    private void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        cam = Camera.main;
        generator = FindObjectOfType<Generator>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void FixedUpdate()
    {
        if (mod)
        {
            if (adding.ReadValue<float>() > 0.001f)
            {
                generator.DispatchShaderWithPoints(cursorPosition, true);
            }
            else if (subtracting.ReadValue<float>() > 0.001f)
            {
                generator.DispatchShaderWithPoints(cursorPosition, false);
            }
        }
    }

    void Update()
    {
        // cruise
        Vector3 c = cruise.ReadValue<Vector3>();
        Vector3 forward = c.z * transform.forward;
        Vector3 right = c.x * transform.right;
        Vector3 up = c.y * transform.up;

        Vector3 vel = (forward + up + right).normalized * (cruiseSpeed * Time.fixedDeltaTime);
        
        rb.velocity = vel;

        if (quit.ReadValue<float>() > 0.01f)
        {
            Application.Quit();
        }
        
        // raycast to point
        float midX = Screen.width / 2f;
        float midY = Screen.height / 2f;
        Vector3 screenPoint = new Vector3(midX, midY, 1);
        Ray cursorRay = cam.ScreenPointToRay(screenPoint);
        RaycastHit hit = new RaycastHit();
        Physics.Raycast(cursorRay, out hit, 100f);
        cursorPosition = hit.point;
        
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

        float b = blocky.ReadValue<float>();
        
        if (b > 0.01f)
        {
            generator.blocky = !generator.blocky;
            generator.UpdateMainmesh();
            blocky.Disable();
            Invoke("SetBlockyTimer", 0.3f);
        }

    }
    
    private void LateUpdate()
    {
        Vector2 delta = mouse.ReadValue<Vector2>();
        float rx = delta.y * rotateSpeed * Time.fixedDeltaTime;
        float ry = delta.x * rotateSpeed * Time.fixedDeltaTime;

        xRotation -= rx;
        xRotation = Mathf.Clamp(xRotation, -90, 90);
        yRotation += ry;

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f).normalized;
    }
    

    IEnumerator SetModTimer()
    {
        modify.Enable();
        return null;
    }
    
    IEnumerator SetBlockyTimer()
    {
        blocky.Enable();
        return null;
    }
    
    private void OnEnable()
    {
        cruise.Enable();
        mouse.Enable();
        quit.Enable();
        modify.Enable();
        adding.Enable();
        subtracting.Enable();
        blocky.Enable();
    }

    private void OnDisable()
    {
        cruise.Disable();
        mouse.Disable();
        quit.Disable();
        modify.Disable();
        adding.Disable();
        subtracting.Disable();
        blocky.Disable();
    }
}

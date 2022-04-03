using System;
using System.Collections;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Controller : MonoBehaviour
{
    [SerializeField] private InputAction modify;
    [SerializeField] private InputAction quit;
    
    [SerializeField] private InputAction cruise;
    [SerializeField] private InputAction mouse;

    [SerializeField] float cruiseSpeed;
    [SerializeField] float rotateSpeed;
    [SerializeField] GameObject cursorHead;

    private Rigidbody rb;
    private Camera cam;

    private float xRotation = 0f;
    private float yRotation = 0f;

    private Vector3 cursorPosition;
    private bool mod = false;
    
    private void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
        cam = Camera.main;
    }
    
    void Update()
    {
        // cruise
        float c = cruise.ReadValue<float>();
        
        Vector3 vel = transform.forward * (c * cruiseSpeed * Time.fixedDeltaTime);
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
    
    private void OnEnable()
    {
        cruise.Enable();
        mouse.Enable();
        quit.Enable();
        modify.Enable();
    }

    private void OnDisable()
    {
        cruise.Disable();
        mouse.Disable();
        quit.Disable();
        modify.Disable();
    }
}

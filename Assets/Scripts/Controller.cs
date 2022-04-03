using System;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Controller : MonoBehaviour
{
    [SerializeField] private InputAction cruise;
    [SerializeField] private InputAction quit;
    [SerializeField] private InputAction mouse;
    
    [SerializeField] private InputAction w;
    [SerializeField] private InputAction a;
    [SerializeField] private InputAction s;
    [SerializeField] private InputAction d;

    [SerializeField] float cruiseSpeed;
    [SerializeField] private float rotateSpeed;

    private Rigidbody rb;

    private float xRotation = 0f;
    private float yRotation = 0f;
    private void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
        Cursor.lockState = CursorLockMode.Locked;
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

    private void OnEnable()
    {
        cruise.Enable();
        mouse.Enable();
        quit.Enable();
        w.Enable();
        a.Enable();
        s.Enable();
        d.Enable();
    }

    private void OnDisable()
    {
        cruise.Disable();
        mouse.Disable();
        quit.Disable();
        w.Disable();
        a.Disable();
        s.Disable();
        d.Disable();
    }
}

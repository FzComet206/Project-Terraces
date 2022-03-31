using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Controller : MonoBehaviour
{
    [SerializeField] private InputAction cruise;
    [SerializeField] private InputAction quit;
    
    [SerializeField] private InputAction w;
    [SerializeField] private InputAction a;
    [SerializeField] private InputAction s;
    [SerializeField] private InputAction d;

    [SerializeField] float cruiseSpeed;
    [SerializeField] private float rotateSpeed;

    private Rigidbody rb; 
    private void Start()
    {
        rb = gameObject.GetComponent<Rigidbody>();
    }
    
    void Update()
    {
        float c = cruise.ReadValue<float>();
        float _w = w.ReadValue<float>();
        float _a = a.ReadValue<float>();
        float _s = s.ReadValue<float>();
        float _d = d.ReadValue<float>();

        float dirX = (_s - _w);
        float dirY = (_d - _a);

        Vector3 rotate = new Vector3(dirX, dirY, 0).normalized;
        rotate *= rotateSpeed * Time.fixedDeltaTime;
        
        Quaternion r = Quaternion.Euler(rotate * 20f);
        transform.rotation *= r;
        
        Vector3 vel = transform.forward * (c * cruiseSpeed * Time.fixedDeltaTime);
        rb.velocity = vel;

        if (quit.ReadValue<float>() > 0.01f)
        {
            Application.Quit();
        }
    }

    private void OnEnable()
    {
        cruise.Enable();
        quit.Enable();
        w.Enable();
        a.Enable();
        s.Enable();
        d.Enable();
    }

    private void OnDisable()
    {
        cruise.Disable();
        quit.Disable();
        w.Disable();
        a.Disable();
        s.Disable();
        d.Disable();
    }
}

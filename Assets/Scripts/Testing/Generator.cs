using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Generator : MonoBehaviour
{
    private int axisN = 64;
    
    private float[] points;

    private void Start()
    {
        points = new float[axisN * axisN * axisN];
        GenerateMainMesh();
    }

    void GenerateMainMesh()
    {
        
    } 
    
    void DispatchShaderWithPoints(float[] points)
    {
        
    }

    IEnumerator CaptureModifyDispatch()
    {
        return null;
    }
}

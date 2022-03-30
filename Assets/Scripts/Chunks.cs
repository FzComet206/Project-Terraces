using System.Collections;
using UnityEngine;

public class Chunks 
{
    // input configs 
    public int index;
    public Vector3 startPos;

    // states
    public bool active;
    public bool generated;

    // object datas
    public GameObject chunk;
    public Vector3[] verticies;
    public int[] triangles;

    public Chunks(int index, Vector3 startPos)
    {
        this.index = index;
        this.startPos = startPos;
    }
}
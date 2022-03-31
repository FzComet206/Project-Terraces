using System;
using UnityEngine;

public class Chunks: IComparable<Chunks>
{
    // input configs 
    public int index;
    public Vector3 startPos;
    public Vector3 centerPos;
    Controller playerRef;

    // states
    public bool active;

    // object datas
    public GameObject chunk;
    public Vector3[] verticies;
    public int[] triangles;

    public Chunks(int index, Vector3 startPos, Vector3 centerPos)
    {
        playerRef = GameObject.FindObjectOfType<Controller>();
        this.index = index;
        this.startPos = startPos;
        this.centerPos = centerPos;
        active = false;
    }

    public int CompareTo(Chunks comparePart)
    {
        Vector3 playerPos = playerRef.transform.position;
        float curr = (playerPos - centerPos).magnitude;
        float next = (playerPos - comparePart.centerPos).magnitude;
        if (curr < next)
        {
            return 1;
        }
        return 0;
    }
}
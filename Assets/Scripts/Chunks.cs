using System;
using UnityEngine;

public class Chunks: IComparable<Chunks>
{
    // input configs 
    public int index;
    public Vector3 startPos;
    public Vector3 centerPos;

    // states
    public bool active;

    // object datas
    public GameObject chunk;
    public Vector3[] verticies;
    public int[] triangles;

    public Chunks(int index, Vector3 startPos, Vector3 centerPos)
    {
        this.index = index;
        this.startPos = startPos;
        this.centerPos = centerPos;
        active = false;
    }

    public int CompareTo(Chunks comparePart)
    {
        if (comparePart == null)
        {
            return 1;
        }
        return comparePart.centerPos.magnitude.CompareTo(this.centerPos.magnitude);
    }
}
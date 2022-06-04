using System;
using UnityEngine;

public class BrushSystem
{
    public BrushSystem()
    {
        // brush types and sizes
    }

    public void EvaluateBrush(Vector3 position)
    {
        // find corresponding int2 and get chunk from dictionary
        // subtract position from chunk position
        // get local point indexes
        
        // if overflow, get direction of overflow and find nearby chunk
        // get nearby chunk local indexes that changed
        
        // update data for each chunk
        // call compute shader to triangulate for each chunk

        throw new NotImplementedException();
    }
}

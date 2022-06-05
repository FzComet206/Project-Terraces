using System;
using System.Drawing.Drawing2D;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public class BrushSystem
{
    public enum BrushType
    {
        SmallSquare
    }
    
    public struct IndexsAndChunk
    {
        public int2 coord;
        public int3 local;
        public IndexsAndChunk(int2 coord, int3 local)
        {
            this.coord = coord;
            this.local = local;
        }
    }

    public BrushType brushType;

    public BrushSystem()
    {
        // init brush system
        brushType = BrushType.SmallSquare;
    }

    public IndexsAndChunk[] EvaluateBrush(Vector3 position)
    {
        switch (brushType)
        {
            case BrushType.SmallSquare:

                IndexsAndChunk[] indexsAndChunksArray = new IndexsAndChunk[27];

                position /= 15;
                
                int x = Mathf.RoundToInt(position.x);
                int y = Mathf.RoundToInt(position.y);
                int z = Mathf.RoundToInt(position.z);

                int counter = 0;
                
                for (int i = -1; i < 2; i++)
                {
                    for (int j = -1; j < 2; j++)
                    {
                        for (int k = -1; k < 2; k++)
                        {
                            int _x = x + i;
                            int _y = y + j;
                            int _z = k + k;
                            
                            // find the coord
                            // check if outside of bound
                            // find local coord of index to the chunk
                            // duplicate coord if necessary

                            IndexsAndChunk indexsAndChunk = new IndexsAndChunk(
                                new int2(-1, -1),
                                new int3(-1, -1, -1)
                                );
                            indexsAndChunksArray[counter] = indexsAndChunk;
                            counter++;
                            // find all corresponding chunks and which set of x y z belongs to which chunk
                        }
                    }
                }

                return indexsAndChunksArray;
                
            default:
                return new IndexsAndChunk[1];
        }
        // find corresponding int2 and get chunk from dictionary
        // subtract position from chunk position
        // get local point indexes
        
        // if overflow, get direction of overflow and find nearby chunk
        // get nearby chunk local indexes that changed
        
        // update data for each chunk
        // call compute shader to triangulate for each chunk
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using UnityEngine;

public class ChunkGenerator : MonoBehaviour
{
    //int chunkIndex;
    Chunk[] chunk;
    void Start()
    {
        chunk = new Chunk[10];
        for(int i = 0; i < 10; i++)
        {
            for (int j = 0; j < 10; j++)
            {
                chunk[i] = new Chunk(new Vector3Int(i * 20, 0, j * 20), transform, i * 10 + j);
            }
        }
    }
    
}

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HelperFunctions : MonoBehaviour
{
    static int[,,,] Initialize4DArrayWithValue(int[,,,] array, int value)
    {
        for (int i = 0; i < array.GetLength(0); i++)
        {
            for (int j = 0; j < array.GetLength(1); j++)
            {
                for (int k = 0; k < array.GetLength(2); k++)
                {
                    for (int l = 0; l < array.GetLength(3); l++)
                    {
                        array[i, j, k, l] = value;
                    }
                }
            }
        }
        return array;
    }

    static int FindMostCommonValue(int[,,,] array)
    {
        Dictionary<int, int> frequencyMap = new Dictionary<int, int>();

        foreach (var element in array)
        {
            if (frequencyMap.ContainsKey(element))
            {
                frequencyMap[element]++;
            }
            else
            {
                frequencyMap[element] = 1;
            }
        }

        int mostCommonValue = frequencyMap.OrderByDescending(pair => pair.Value).First().Key;
        return mostCommonValue;
    }

    /*
     void MarchCube(Vector3Int position)
    {
        float[] cube = new float[8];
        for (int i = 0; i < 8; i++)
        {

            cube[i] = SampleTerrain(position + CornerTable[i]);
        }

        int configIndex = GetCubeCongif(cube);

        if (configIndex == 0 || configIndex == 255) return;

        for (int edgeIndex = 0; edgeIndex < 15; edgeIndex++)
        {
            int ind = TriangleTable[configIndex, edgeIndex];

            if (ind == -1) return;

            Vector3 vert1 = position + CornerTable[EdgeTable[ind, 0]];
            Vector3 vert2 = position + CornerTable[EdgeTable[ind, 1]];

            Vector3 vertPos;
            if (smoothTerrain)
            {
                float vert1Sample = cube[EdgeTable[ind, 0]];
                float vert2Sample = cube[EdgeTable[ind, 1]];

                float difference = vert2Sample - vert1Sample;

                if (difference == 0)
                {
                    Debug.Log("DIFFERENCE IS 0 AND IT SHOULD NEVER HAPPEN !!!");
                }

                difference = (terrainHeight - vert1Sample) / difference;
                
                vertPos = vert1 + (vert2 - vert1) * difference;
            }
            else
            {
                vertPos = (vert1 + vert2) / 2f;
            }

            
            counter++;
            int indexof = vertices.IndexOf(vertPos);
            if (indexof != -1)
            {
                triangles.Add(indexof);
            }
            else 
            {
                vertices.Add(vertPos);
                triangles.Add(vertices.Count - 1); 
            }
            

            //vertices.Add(vertPos);
            //triangles.Add(vertices.Count - 1);

        }

    }
     */
}

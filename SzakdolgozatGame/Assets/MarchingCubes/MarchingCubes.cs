using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

public class MarchingCubes : MonoBehaviour
{
    MeshFilter meshFilter;
    MeshCollider meshCollider;

    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();

    [SerializeField] int xLength = 20;
    [SerializeField] int yLength = 20;
    [SerializeField] int zLength = 20;
    float[,,] terrainData;
    int terrainTriangeIndex = 0;
    float t = 0f;
    float max, min;
    bool dostuff = false;
    [SerializeField] float terrainHeight = 0.5f;
    [SerializeField] float valueDisplayFontSize = 1f;

    [SerializeField] bool smoothTerrain = true;

    [SerializeField] GameObject DebugParent = null;
    [SerializeField] bool displayValues = true;
    [SerializeField] int displayPlane = 0;
    int displayPlanePrev = 0;
    GameObject[,] valueDisplay;
    TextMeshPro[,] valueDisplayText;

    int counter = 0;

    //int _configIndex = -1;

    void Start()
    {
        Debug.Log("rows: " + TriangleTable.Length);
        Debug.Log("cols: " + TriangleTable.GetLength(1));
        max = yLength;
        min = -yLength;

        if (displayValues)
        {
            valueDisplay = new GameObject[xLength + 1, yLength + 1];
            valueDisplayText = new TextMeshPro[xLength + 1, yLength + 1];
            for (int i = 0; i < xLength + 1; i++)
            {
                for (int j = 0; j < yLength + 1; j++)
                {
                    for (int k = 0; k < zLength + 1; k++)
                    {
                        valueDisplay[i, j] = new GameObject();
                        valueDisplay[i, j].transform.parent = DebugParent.transform;
                    }
                }
            }
            for (int i = 0; i < xLength + 1; i++)
            {
                for (int j = 0; j < yLength + 1; j++)
                {
                    for (int k = 0; k < zLength + 1; k++)
                    {
                        valueDisplay[i, j].AddComponent<TextMeshPro>();
                        valueDisplayText[i, j] = valueDisplay[i, j].GetComponent<TextMeshPro>();
                        valueDisplayText[i, j].fontSize = valueDisplayFontSize;
                        valueDisplayText[i, j].color = Color.black;
                        valueDisplayText[i, j].horizontalAlignment = HorizontalAlignmentOptions.Center;
                        valueDisplayText[i, j].verticalAlignment = VerticalAlignmentOptions.Middle;
                    }
                }
            }
        }

        meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null) Debug.Log("MeshFilter is NULL!");
        meshCollider = GetComponent<MeshCollider>();
        if (meshFilter == null) Debug.Log("MeshCollider is NULL!");
        CreateTerrain();
        CreateMeshData();
        BuildMesh();

    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            Debug.Log("Mesh Rebuilt");
            BuildMesh();
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            dostuff = !dostuff;
        }
        if (dostuff)
        {
            terrainHeight = Mathf.Lerp(min, max, t);
            t += 0.1f * Time.deltaTime;
            if (t > 1f)
            {
                float temp = min;
                min = max;
                max = temp;
                t = 0f;
            }
            ClearData();
            CreateMeshData();
            Debug.Log("HEIGHT: " + (int)terrainHeight + " TRIANGLES: " + triangles.Count / 3);
            BuildMesh();
        }
        if (Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            t += 0.01f;
            terrainHeight = Mathf.Lerp(-yLength, yLength, t);
            ClearData();
            CreateMeshData();
            Debug.Log("HEIGHT: " + (int)terrainHeight + " TRIANGLES: " + triangles.Count / 3);
            BuildMesh();
        }
        if (Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            t -= 0.01f;
            terrainHeight = Mathf.Lerp(-yLength, yLength, t);
            ClearData();
            CreateMeshData();
            Debug.Log("HEIGHT: " + (int)terrainHeight + " TRIANGLES: " + triangles.Count / 3);
            BuildMesh();
        }
        if (Input.GetKeyDown(KeyCode.R) && displayValues)
        {
            for (int i = 0; i < xLength + 1; i++)
            {
                for (int j = 0; j < yLength + 1; j++)
                {
                    valueDisplayText[i, j].fontSize = valueDisplayFontSize;
                    valueDisplayText[i, j].text = SampleTerrain(new Vector3Int(i, j, displayPlane)).ToString();
                    valueDisplay[i, j].transform.position = new Vector3Int(i, j, displayPlane);

                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(new Vector3(xLength/2, yLength/2, zLength/2), new Vector3(xLength, yLength, zLength));
    }

    private void OnDrawGizmosSelected()
    {
        return;
        for (int x = 0; x < xLength; x++)
        {
            for (int y = 0; y < yLength; y++)
            {
                for (int z = 0; z < zLength; z++)
                {
                    if(terrainData[x, y, z] >= 0f && terrainData[x, y, z] <= 1f)
                    {
                        //Gizmos.color = Color.blue;
                        //Gizmos.DrawWireCube(new Vector3(x + 0.5f, y + 0.5f, z + 0.5f), new Vector3(1, 1, 1));
                        if (terrainData[x, y, z] < 0.5f)
                        {
                            Gizmos.color = Color.red;
                            Gizmos.DrawSphere(new Vector3(x, y, z), 0.1f);
                        } 
                        else
                        {
                            
                            Gizmos.color = Color.green;
                            Gizmos.DrawSphere(new Vector3(x, y, z), 0.1f);
                        }
                    }
                }
            }
        }
    }

    void CreateTerrain()
    {
        

        float noiseValue;
        float currentHeight = 3;
        terrainData = new float[xLength + 1, yLength + 1, zLength + 1];
        for (int x = 0; x < xLength + 1; x++)
        {
            for (int z = 0; z < zLength + 1; z++)
            {
                //noiseValue = Mathf.PerlinNoise((float)x / 16f * 1.5f, (float)z / 16f * 1.5f);
                //currentHeight = yLength * noiseValue;
                
                for (int y = 0; y < yLength + 1; y++)
                {
                    noiseValue = 0;
                    noiseValue += Mathf.PerlinNoise((float)x / 16f * 1.5f, (float)y / 16f * 1.5f);
                    noiseValue += Mathf.PerlinNoise((float)y / 16f * 1.5f, (float)z / 16f * 1.5f);
                    noiseValue += Mathf.PerlinNoise((float)x / 16f * 1.5f, (float)z / 16f * 1.5f);

                    noiseValue += Mathf.PerlinNoise((float)y / 16f * 1.5f, (float)x / 16f * 1.5f);
                    noiseValue += Mathf.PerlinNoise((float)z / 16f * 1.5f, (float)y / 16f * 1.5f);
                    noiseValue += Mathf.PerlinNoise((float)z / 16f * 1.5f, (float)x / 16f * 1.5f);

                    noiseValue /= 3f;

                    currentHeight = yLength * noiseValue;
                    terrainData[x, y, z] = (float)y - currentHeight;
                    //terrainData[x, y, z] = noiseValue;

                }
            }
        }
    }

    void CreateMeshData()
    {
        int[,,,] vertexIndexArray = new int[xLength, yLength, zLength, 16];
        //vertexIndexArray = Initialize4DArrayWithValue(vertexIndexArray, 0);
        for (int x = 0; x < xLength; x++)
        {
            for (int y = 0; y < yLength; y++)
            {
                for (int z = 0; z < zLength; z++)
                {
                    Vector3Int position = new Vector3Int(x, y, z);
                    //----------------------------------------------------------------------------------------------------
                    float[] cube = new float[8];
                    for (int i = 0; i < 8; i++)
                    {
                        cube[i] = SampleTerrain(position + CornerTable[i]);
                    }

                    int configIndex = GetCubeCongif(cube);

                    if (configIndex == 0 || configIndex == 255) continue;

                    for (int edgeCounter = 0; edgeCounter < 16; edgeCounter++)
                    {
                        int edgeIndex = TriangleTable[configIndex, edgeCounter];

                        if (edgeIndex == -1) break;

                        switch (edgeIndex)
                        {
                            case 0:
                                if (y > 0)
                                {
                                    vertexIndexArray[x, y, z, edgeIndex] = vertexIndexArray[x, y - 1, z, 2];
                                    triangles.Add(vertexIndexArray[x, y - 1, z, 2]);
                                }
                                else if (z > 0)
                                {
                                    vertexIndexArray[x, y, z, edgeIndex] = vertexIndexArray[x, y, z - 1, 4];
                                    triangles.Add(vertexIndexArray[x, y, z - 1, 4]);
                                }
                                else
                                    vertexIndexArray[x, y, z, edgeIndex] = CalculateVertex(position, edgeIndex, cube);
                                break;
                            case 1:
                                if (z > 0)
                                {
                                    vertexIndexArray[x, y, z, edgeIndex] = vertexIndexArray[x, y, z - 1, 5];
                                    triangles.Add(vertexIndexArray[x, y, z - 1, 5]);
                                }
                                else
                                    vertexIndexArray[x, y, z, edgeIndex] = CalculateVertex(position, edgeIndex, cube);
                                break;
                            case 2:
                                if (z > 0)
                                {
                                    vertexIndexArray[x, y, z, edgeIndex] = vertexIndexArray[x, y, z - 1, 6];
                                    triangles.Add(vertexIndexArray[x, y, z - 1, 6]);
                                }
                                else
                                    vertexIndexArray[x, y, z, edgeIndex] = CalculateVertex(position, edgeIndex, cube);
                                break;
                            case 3:
                                if (x > 0)
                                {
                                    vertexIndexArray[x, y, z, edgeIndex] = vertexIndexArray[x - 1, y, z, 1];
                                    triangles.Add(vertexIndexArray[x - 1, y, z, 1]);
                                }
                                else if (z > 0)
                                {
                                    vertexIndexArray[x, y, z, edgeIndex] = vertexIndexArray[x, y, z - 1, 7];
                                    triangles.Add(vertexIndexArray[x, y, z - 1, 7]);
                                }
                                else
                                    vertexIndexArray[x, y, z, edgeIndex] = CalculateVertex(position, edgeIndex, cube);
                                break;
                            case 4:
                                if (y > 0)
                                {
                                    vertexIndexArray[x, y, z, edgeIndex] = vertexIndexArray[x, y - 1, z, 6];
                                    triangles.Add(vertexIndexArray[x, y - 1, z, 6]);
                                }
                                else
                                    vertexIndexArray[x, y, z, edgeIndex] = CalculateVertex(position, edgeIndex, cube);
                                break;
                            case 7:
                                if (x > 0)
                                {
                                    vertexIndexArray[x, y, z, edgeIndex] = vertexIndexArray[x - 1, y, z, 5];
                                    triangles.Add(vertexIndexArray[x - 1, y, z, 5]);
                                }
                                else
                                    vertexIndexArray[x, y, z, edgeIndex] = CalculateVertex(position, edgeIndex, cube);
                                break;
                            case 8:
                                if (x > 0)
                                {
                                    vertexIndexArray[x, y, z, edgeIndex] = vertexIndexArray[x - 1, y, z, 9];
                                    triangles.Add(vertexIndexArray[x - 1, y, z, 9]);
                                }
                                else if (y > 0)
                                {
                                    vertexIndexArray[x, y, z, edgeIndex] = vertexIndexArray[x, y - 1, z, 11];
                                    triangles.Add(vertexIndexArray[x, y - 1, z, 11]);
                                }
                                else
                                    vertexIndexArray[x, y, z, edgeIndex] = CalculateVertex(position, edgeIndex, cube);
                                break;
                            case 9:
                                if (y > 0)
                                {
                                    vertexIndexArray[x, y, z, edgeIndex] = vertexIndexArray[x, y - 1, z, 10];
                                    triangles.Add(vertexIndexArray[x, y - 1, z, 10]);
                                }
                                else
                                    vertexIndexArray[x, y, z, edgeIndex] = CalculateVertex(position, edgeIndex, cube);
                                break;
                            case 11:
                                if (x > 0)
                                {
                                    vertexIndexArray[x, y, z, edgeIndex] = vertexIndexArray[x - 1, y, z, 10];
                                    triangles.Add(vertexIndexArray[x - 1, y, z, 10]);
                                }
                                else
                                    vertexIndexArray[x, y, z, edgeIndex] = CalculateVertex(position, edgeIndex, cube);
                                break;
                            default:
                                vertexIndexArray[x, y, z, edgeIndex] = CalculateVertex(position, edgeIndex, cube);
                                //Debug.Log(edgeIndex);
                                break;
                        }
                    }
                }
            }
        }
        Debug.Log("triangle indices: " + triangles.Count);
        //Debug.Log("----->" + FindMostCommonValue(vertexIndexArray));
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

    int CalculateVertex(Vector3Int position, int index, float[] cube)
    {
        
        Vector3 vert1 = position + CornerTable[EdgeTable[index, 0]];
        Vector3 vert2 = position + CornerTable[EdgeTable[index, 1]];

        Vector3 vertPos;
        if (smoothTerrain)
        {
            float vert1Sample = cube[EdgeTable[index, 0]];
            float vert2Sample = cube[EdgeTable[index, 1]];

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

        vertices.Add(vertPos);
        triangles.Add(vertices.Count - 1);

        return (vertices.Count - 1);
    }

    int GetCubeCongif(float[] cube)
    {
        int configIndex = 0;
        for (int i = 0; i < 8; i++)
        {
            //Debug.Log(cube[i]);
            if (cube[i] > terrainHeight)
            {
                configIndex |= 1 << i;
            }
        }
        return configIndex;
    }

    float SampleTerrain(Vector3Int point)
    {
        return terrainData[point.x, point.y, point.z];
    }

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

    void ClearData()
    {
        vertices.Clear();
        triangles.Clear();
    }

    void BuildMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
        
    }

    int[,,,] Initialize4DArrayWithValue(int[,,,] array, int value)
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

    private readonly Vector3Int[] CornerTable = new Vector3Int[8] {

        new Vector3Int(0, 0, 0),
        new Vector3Int(1, 0, 0),
        new Vector3Int(1, 1, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, 0, 1),
        new Vector3Int(1, 0, 1),
        new Vector3Int(1, 1, 1),
        new Vector3Int(0, 1, 1) 

    };

    private readonly int[,] EdgeTable = new int[12, 2] {

        {0, 1},
        {1, 2},
        {3, 2},
        {0, 3},
        {4, 5},
        {5, 6},
        {7, 6},
        {4, 7},
        {0, 4},
        {1, 5},
        {2, 6},
        {3, 7}

        //{ new Vector3(0.0f, 0.0f, 0.0f), new Vector3(1.0f, 0.0f, 0.0f) },
        //{ new Vector3(1.0f, 0.0f, 0.0f), new Vector3(1.0f, 1.0f, 0.0f) },
        //{ new Vector3(0.0f, 1.0f, 0.0f), new Vector3(1.0f, 1.0f, 0.0f) },
        //{ new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 1.0f, 0.0f) },
        //{ new Vector3(0.0f, 0.0f, 1.0f), new Vector3(1.0f, 0.0f, 1.0f) },
        //{ new Vector3(1.0f, 0.0f, 1.0f), new Vector3(1.0f, 1.0f, 1.0f) },
        //{ new Vector3(0.0f, 1.0f, 1.0f), new Vector3(1.0f, 1.0f, 1.0f) },
        //{ new Vector3(0.0f, 0.0f, 1.0f), new Vector3(0.0f, 1.0f, 1.0f) },
        //{ new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.0f, 0.0f, 1.0f) },
        //{ new Vector3(1.0f, 0.0f, 0.0f), new Vector3(1.0f, 0.0f, 1.0f) },
        //{ new Vector3(1.0f, 1.0f, 0.0f), new Vector3(1.0f, 1.0f, 1.0f) },
        //{ new Vector3(0.0f, 1.0f, 0.0f), new Vector3(0.0f, 1.0f, 1.0f) }

    };

    private readonly int[,] TriangleTable = new int[,] {

        {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {0, 1, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {1, 8, 3, 9, 8, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {0, 8, 3, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {9, 2, 10, 0, 2, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {2, 8, 3, 2, 10, 8, 10, 9, 8, -1, -1, -1, -1, -1, -1, -1},
        {3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {0, 11, 2, 8, 11, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {1, 9, 0, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {1, 11, 2, 1, 9, 11, 9, 8, 11, -1, -1, -1, -1, -1, -1, -1},
        {3, 10, 1, 11, 10, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {0, 10, 1, 0, 8, 10, 8, 11, 10, -1, -1, -1, -1, -1, -1, -1},
        {3, 9, 0, 3, 11, 9, 11, 10, 9, -1, -1, -1, -1, -1, -1, -1},
        {9, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {4, 3, 0, 7, 3, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {0, 1, 9, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {4, 1, 9, 4, 7, 1, 7, 3, 1, -1, -1, -1, -1, -1, -1, -1},
        {1, 2, 10, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {3, 4, 7, 3, 0, 4, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1},
        {9, 2, 10, 9, 0, 2, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1},
        {2, 10, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4, -1, -1, -1, -1},
        {8, 4, 7, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {11, 4, 7, 11, 2, 4, 2, 0, 4, -1, -1, -1, -1, -1, -1, -1},
        {9, 0, 1, 8, 4, 7, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1},
        {4, 7, 11, 9, 4, 11, 9, 11, 2, 9, 2, 1, -1, -1, -1, -1},
        {3, 10, 1, 3, 11, 10, 7, 8, 4, -1, -1, -1, -1, -1, -1, -1},
        {1, 11, 10, 1, 4, 11, 1, 0, 4, 7, 11, 4, -1, -1, -1, -1},
        {4, 7, 8, 9, 0, 11, 9, 11, 10, 11, 0, 3, -1, -1, -1, -1},
        {4, 7, 11, 4, 11, 9, 9, 11, 10, -1, -1, -1, -1, -1, -1, -1},
        {9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {9, 5, 4, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {0, 5, 4, 1, 5, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {8, 5, 4, 8, 3, 5, 3, 1, 5, -1, -1, -1, -1, -1, -1, -1},
        {1, 2, 10, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {3, 0, 8, 1, 2, 10, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1},
        {5, 2, 10, 5, 4, 2, 4, 0, 2, -1, -1, -1, -1, -1, -1, -1},
        {2, 10, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8, -1, -1, -1, -1},
        {9, 5, 4, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {0, 11, 2, 0, 8, 11, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1},
        {0, 5, 4, 0, 1, 5, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1},
        {2, 1, 5, 2, 5, 8, 2, 8, 11, 4, 8, 5, -1, -1, -1, -1},
        {10, 3, 11, 10, 1, 3, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1},
        {4, 9, 5, 0, 8, 1, 8, 10, 1, 8, 11, 10, -1, -1, -1, -1},
        {5, 4, 0, 5, 0, 11, 5, 11, 10, 11, 0, 3, -1, -1, -1, -1},
        {5, 4, 8, 5, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1},
        {9, 7, 8, 5, 7, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {9, 3, 0, 9, 5, 3, 5, 7, 3, -1, -1, -1, -1, -1, -1, -1},
        {0, 7, 8, 0, 1, 7, 1, 5, 7, -1, -1, -1, -1, -1, -1, -1},
        {1, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {9, 7, 8, 9, 5, 7, 10, 1, 2, -1, -1, -1, -1, -1, -1, -1},
        {10, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3, -1, -1, -1, -1},
        {8, 0, 2, 8, 2, 5, 8, 5, 7, 10, 5, 2, -1, -1, -1, -1},
        {2, 10, 5, 2, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1},
        {7, 9, 5, 7, 8, 9, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1},
        {9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7, 11, -1, -1, -1, -1},
        {2, 3, 11, 0, 1, 8, 1, 7, 8, 1, 5, 7, -1, -1, -1, -1},
        {11, 2, 1, 11, 1, 7, 7, 1, 5, -1, -1, -1, -1, -1, -1, -1},
        {9, 5, 8, 8, 5, 7, 10, 1, 3, 10, 3, 11, -1, -1, -1, -1},
        {5, 7, 0, 5, 0, 9, 7, 11, 0, 1, 0, 10, 11, 10, 0, -1},
        {11, 10, 0, 11, 0, 3, 10, 5, 0, 8, 0, 7, 5, 7, 0, -1},
        {11, 10, 5, 7, 11, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {0, 8, 3, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {9, 0, 1, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {1, 8, 3, 1, 9, 8, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1},
        {1, 6, 5, 2, 6, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {1, 6, 5, 1, 2, 6, 3, 0, 8, -1, -1, -1, -1, -1, -1, -1},
        {9, 6, 5, 9, 0, 6, 0, 2, 6, -1, -1, -1, -1, -1, -1, -1},
        {5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8, -1, -1, -1, -1},
        {2, 3, 11, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {11, 0, 8, 11, 2, 0, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1},
        {0, 1, 9, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1},
        {5, 10, 6, 1, 9, 2, 9, 11, 2, 9, 8, 11, -1, -1, -1, -1},
        {6, 3, 11, 6, 5, 3, 5, 1, 3, -1, -1, -1, -1, -1, -1, -1},
        {0, 8, 11, 0, 11, 5, 0, 5, 1, 5, 11, 6, -1, -1, -1, -1},
        {3, 11, 6, 0, 3, 6, 0, 6, 5, 0, 5, 9, -1, -1, -1, -1},
        {6, 5, 9, 6, 9, 11, 11, 9, 8, -1, -1, -1, -1, -1, -1, -1},
        {5, 10, 6, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {4, 3, 0, 4, 7, 3, 6, 5, 10, -1, -1, -1, -1, -1, -1, -1},
        {1, 9, 0, 5, 10, 6, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1},
        {10, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4, -1, -1, -1, -1},
        {6, 1, 2, 6, 5, 1, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1},
        {1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7, -1, -1, -1, -1},
        {8, 4, 7, 9, 0, 5, 0, 6, 5, 0, 2, 6, -1, -1, -1, -1},
        {7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9, -1},
        {3, 11, 2, 7, 8, 4, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1},
        {5, 10, 6, 4, 7, 2, 4, 2, 0, 2, 7, 11, -1, -1, -1, -1},
        {0, 1, 9, 4, 7, 8, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1},
        {9, 2, 1, 9, 11, 2, 9, 4, 11, 7, 11, 4, 5, 10, 6, -1},
        {8, 4, 7, 3, 11, 5, 3, 5, 1, 5, 11, 6, -1, -1, -1, -1},
        {5, 1, 11, 5, 11, 6, 1, 0, 11, 7, 11, 4, 0, 4, 11, -1},
        {0, 5, 9, 0, 6, 5, 0, 3, 6, 11, 6, 3, 8, 4, 7, -1},
        {6, 5, 9, 6, 9, 11, 4, 7, 9, 7, 11, 9, -1, -1, -1, -1},
        {10, 4, 9, 6, 4, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {4, 10, 6, 4, 9, 10, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1},
        {10, 0, 1, 10, 6, 0, 6, 4, 0, -1, -1, -1, -1, -1, -1, -1},
        {8, 3, 1, 8, 1, 6, 8, 6, 4, 6, 1, 10, -1, -1, -1, -1},
        {1, 4, 9, 1, 2, 4, 2, 6, 4, -1, -1, -1, -1, -1, -1, -1},
        {3, 0, 8, 1, 2, 9, 2, 4, 9, 2, 6, 4, -1, -1, -1, -1},
        {0, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {8, 3, 2, 8, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1},
        {10, 4, 9, 10, 6, 4, 11, 2, 3, -1, -1, -1, -1, -1, -1, -1},
        {0, 8, 2, 2, 8, 11, 4, 9, 10, 4, 10, 6, -1, -1, -1, -1},
        {3, 11, 2, 0, 1, 6, 0, 6, 4, 6, 1, 10, -1, -1, -1, -1},
        {6, 4, 1, 6, 1, 10, 4, 8, 1, 2, 1, 11, 8, 11, 1, -1},
        {9, 6, 4, 9, 3, 6, 9, 1, 3, 11, 6, 3, -1, -1, -1, -1},
        {8, 11, 1, 8, 1, 0, 11, 6, 1, 9, 1, 4, 6, 4, 1, -1},
        {3, 11, 6, 3, 6, 0, 0, 6, 4, -1, -1, -1, -1, -1, -1, -1},
        {6, 4, 8, 11, 6, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {7, 10, 6, 7, 8, 10, 8, 9, 10, -1, -1, -1, -1, -1, -1, -1},
        {0, 7, 3, 0, 10, 7, 0, 9, 10, 6, 7, 10, -1, -1, -1, -1},
        {10, 6, 7, 1, 10, 7, 1, 7, 8, 1, 8, 0, -1, -1, -1, -1},
        {10, 6, 7, 10, 7, 1, 1, 7, 3, -1, -1, -1, -1, -1, -1, -1},
        {1, 2, 6, 1, 6, 8, 1, 8, 9, 8, 6, 7, -1, -1, -1, -1},
        {2, 6, 9, 2, 9, 1, 6, 7, 9, 0, 9, 3, 7, 3, 9, -1},
        {7, 8, 0, 7, 0, 6, 6, 0, 2, -1, -1, -1, -1, -1, -1, -1},
        {7, 3, 2, 6, 7, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {2, 3, 11, 10, 6, 8, 10, 8, 9, 8, 6, 7, -1, -1, -1, -1},
        {2, 0, 7, 2, 7, 11, 0, 9, 7, 6, 7, 10, 9, 10, 7, -1},
        {1, 8, 0, 1, 7, 8, 1, 10, 7, 6, 7, 10, 2, 3, 11, -1},
        {11, 2, 1, 11, 1, 7, 10, 6, 1, 6, 7, 1, -1, -1, -1, -1},
        {8, 9, 6, 8, 6, 7, 9, 1, 6, 11, 6, 3, 1, 3, 6, -1},
        {0, 9, 1, 11, 6, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {7, 8, 0, 7, 0, 6, 3, 11, 0, 11, 6, 0, -1, -1, -1, -1},
        {7, 11, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {3, 0, 8, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {0, 1, 9, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {8, 1, 9, 8, 3, 1, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1},
        {10, 1, 2, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {1, 2, 10, 3, 0, 8, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1},
        {2, 9, 0, 2, 10, 9, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1},
        {6, 11, 7, 2, 10, 3, 10, 8, 3, 10, 9, 8, -1, -1, -1, -1},
        {7, 2, 3, 6, 2, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {7, 0, 8, 7, 6, 0, 6, 2, 0, -1, -1, -1, -1, -1, -1, -1},
        {2, 7, 6, 2, 3, 7, 0, 1, 9, -1, -1, -1, -1, -1, -1, -1},
        {1, 6, 2, 1, 8, 6, 1, 9, 8, 8, 7, 6, -1, -1, -1, -1},
        {10, 7, 6, 10, 1, 7, 1, 3, 7, -1, -1, -1, -1, -1, -1, -1},
        {10, 7, 6, 1, 7, 10, 1, 8, 7, 1, 0, 8, -1, -1, -1, -1},
        {0, 3, 7, 0, 7, 10, 0, 10, 9, 6, 10, 7, -1, -1, -1, -1},
        {7, 6, 10, 7, 10, 8, 8, 10, 9, -1, -1, -1, -1, -1, -1, -1},
        {6, 8, 4, 11, 8, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {3, 6, 11, 3, 0, 6, 0, 4, 6, -1, -1, -1, -1, -1, -1, -1},
        {8, 6, 11, 8, 4, 6, 9, 0, 1, -1, -1, -1, -1, -1, -1, -1},
        {9, 4, 6, 9, 6, 3, 9, 3, 1, 11, 3, 6, -1, -1, -1, -1},
        {6, 8, 4, 6, 11, 8, 2, 10, 1, -1, -1, -1, -1, -1, -1, -1},
        {1, 2, 10, 3, 0, 11, 0, 6, 11, 0, 4, 6, -1, -1, -1, -1},
        {4, 11, 8, 4, 6, 11, 0, 2, 9, 2, 10, 9, -1, -1, -1, -1},
        {10, 9, 3, 10, 3, 2, 9, 4, 3, 11, 3, 6, 4, 6, 3, -1},
        {8, 2, 3, 8, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1},
        {0, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {1, 9, 0, 2, 3, 4, 2, 4, 6, 4, 3, 8, -1, -1, -1, -1},
        {1, 9, 4, 1, 4, 2, 2, 4, 6, -1, -1, -1, -1, -1, -1, -1},
        {8, 1, 3, 8, 6, 1, 8, 4, 6, 6, 10, 1, -1, -1, -1, -1},
        {10, 1, 0, 10, 0, 6, 6, 0, 4, -1, -1, -1, -1, -1, -1, -1},
        {4, 6, 3, 4, 3, 8, 6, 10, 3, 0, 3, 9, 10, 9, 3, -1},
        {10, 9, 4, 6, 10, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {4, 9, 5, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {0, 8, 3, 4, 9, 5, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1},
        {5, 0, 1, 5, 4, 0, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1},
        {11, 7, 6, 8, 3, 4, 3, 5, 4, 3, 1, 5, -1, -1, -1, -1},
        {9, 5, 4, 10, 1, 2, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1},
        {6, 11, 7, 1, 2, 10, 0, 8, 3, 4, 9, 5, -1, -1, -1, -1},
        {7, 6, 11, 5, 4, 10, 4, 2, 10, 4, 0, 2, -1, -1, -1, -1},
        {3, 4, 8, 3, 5, 4, 3, 2, 5, 10, 5, 2, 11, 7, 6, -1},
        {7, 2, 3, 7, 6, 2, 5, 4, 9, -1, -1, -1, -1, -1, -1, -1},
        {9, 5, 4, 0, 8, 6, 0, 6, 2, 6, 8, 7, -1, -1, -1, -1},
        {3, 6, 2, 3, 7, 6, 1, 5, 0, 5, 4, 0, -1, -1, -1, -1},
        {6, 2, 8, 6, 8, 7, 2, 1, 8, 4, 8, 5, 1, 5, 8, -1},
        {9, 5, 4, 10, 1, 6, 1, 7, 6, 1, 3, 7, -1, -1, -1, -1},
        {1, 6, 10, 1, 7, 6, 1, 0, 7, 8, 7, 0, 9, 5, 4, -1},
        {4, 0, 10, 4, 10, 5, 0, 3, 10, 6, 10, 7, 3, 7, 10, -1},
        {7, 6, 10, 7, 10, 8, 5, 4, 10, 4, 8, 10, -1, -1, -1, -1},
        {6, 9, 5, 6, 11, 9, 11, 8, 9, -1, -1, -1, -1, -1, -1, -1},
        {3, 6, 11, 0, 6, 3, 0, 5, 6, 0, 9, 5, -1, -1, -1, -1},
        {0, 11, 8, 0, 5, 11, 0, 1, 5, 5, 6, 11, -1, -1, -1, -1},
        {6, 11, 3, 6, 3, 5, 5, 3, 1, -1, -1, -1, -1, -1, -1, -1},
        {1, 2, 10, 9, 5, 11, 9, 11, 8, 11, 5, 6, -1, -1, -1, -1},
        {0, 11, 3, 0, 6, 11, 0, 9, 6, 5, 6, 9, 1, 2, 10, -1},
        {11, 8, 5, 11, 5, 6, 8, 0, 5, 10, 5, 2, 0, 2, 5, -1},
        {6, 11, 3, 6, 3, 5, 2, 10, 3, 10, 5, 3, -1, -1, -1, -1},
        {5, 8, 9, 5, 2, 8, 5, 6, 2, 3, 8, 2, -1, -1, -1, -1},
        {9, 5, 6, 9, 6, 0, 0, 6, 2, -1, -1, -1, -1, -1, -1, -1},
        {1, 5, 8, 1, 8, 0, 5, 6, 8, 3, 8, 2, 6, 2, 8, -1},
        {1, 5, 6, 2, 1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {1, 3, 6, 1, 6, 10, 3, 8, 6, 5, 6, 9, 8, 9, 6, -1},
        {10, 1, 0, 10, 0, 6, 9, 5, 0, 5, 6, 0, -1, -1, -1, -1},
        {0, 3, 8, 5, 6, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {10, 5, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {11, 5, 10, 7, 5, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {11, 5, 10, 11, 7, 5, 8, 3, 0, -1, -1, -1, -1, -1, -1, -1},
        {5, 11, 7, 5, 10, 11, 1, 9, 0, -1, -1, -1, -1, -1, -1, -1},
        {10, 7, 5, 10, 11, 7, 9, 8, 1, 8, 3, 1, -1, -1, -1, -1},
        {11, 1, 2, 11, 7, 1, 7, 5, 1, -1, -1, -1, -1, -1, -1, -1},
        {0, 8, 3, 1, 2, 7, 1, 7, 5, 7, 2, 11, -1, -1, -1, -1},
        {9, 7, 5, 9, 2, 7, 9, 0, 2, 2, 11, 7, -1, -1, -1, -1},
        {7, 5, 2, 7, 2, 11, 5, 9, 2, 3, 2, 8, 9, 8, 2, -1},
        {2, 5, 10, 2, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1},
        {8, 2, 0, 8, 5, 2, 8, 7, 5, 10, 2, 5, -1, -1, -1, -1},
        {9, 0, 1, 5, 10, 3, 5, 3, 7, 3, 10, 2, -1, -1, -1, -1},
        {9, 8, 2, 9, 2, 1, 8, 7, 2, 10, 2, 5, 7, 5, 2, -1},
        {1, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {0, 8, 7, 0, 7, 1, 1, 7, 5, -1, -1, -1, -1, -1, -1, -1},
        {9, 0, 3, 9, 3, 5, 5, 3, 7, -1, -1, -1, -1, -1, -1, -1},
        {9, 8, 7, 5, 9, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {5, 8, 4, 5, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1},
        {5, 0, 4, 5, 11, 0, 5, 10, 11, 11, 3, 0, -1, -1, -1, -1},
        {0, 1, 9, 8, 4, 10, 8, 10, 11, 10, 4, 5, -1, -1, -1, -1},
        {10, 11, 4, 10, 4, 5, 11, 3, 4, 9, 4, 1, 3, 1, 4, -1},
        {2, 5, 1, 2, 8, 5, 2, 11, 8, 4, 5, 8, -1, -1, -1, -1},
        {0, 4, 11, 0, 11, 3, 4, 5, 11, 2, 11, 1, 5, 1, 11, -1},
        {0, 2, 5, 0, 5, 9, 2, 11, 5, 4, 5, 8, 11, 8, 5, -1},
        {9, 4, 5, 2, 11, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {2, 5, 10, 3, 5, 2, 3, 4, 5, 3, 8, 4, -1, -1, -1, -1},
        {5, 10, 2, 5, 2, 4, 4, 2, 0, -1, -1, -1, -1, -1, -1, -1},
        {3, 10, 2, 3, 5, 10, 3, 8, 5, 4, 5, 8, 0, 1, 9, -1},
        {5, 10, 2, 5, 2, 4, 1, 9, 2, 9, 4, 2, -1, -1, -1, -1},
        {8, 4, 5, 8, 5, 3, 3, 5, 1, -1, -1, -1, -1, -1, -1, -1},
        {0, 4, 5, 1, 0, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {8, 4, 5, 8, 5, 3, 9, 0, 5, 0, 3, 5, -1, -1, -1, -1},
        {9, 4, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {4, 11, 7, 4, 9, 11, 9, 10, 11, -1, -1, -1, -1, -1, -1, -1},
        {0, 8, 3, 4, 9, 7, 9, 11, 7, 9, 10, 11, -1, -1, -1, -1},
        {1, 10, 11, 1, 11, 4, 1, 4, 0, 7, 4, 11, -1, -1, -1, -1},
        {3, 1, 4, 3, 4, 8, 1, 10, 4, 7, 4, 11, 10, 11, 4, -1},
        {4, 11, 7, 9, 11, 4, 9, 2, 11, 9, 1, 2, -1, -1, -1, -1},
        {9, 7, 4, 9, 11, 7, 9, 1, 11, 2, 11, 1, 0, 8, 3, -1},
        {11, 7, 4, 11, 4, 2, 2, 4, 0, -1, -1, -1, -1, -1, -1, -1},
        {11, 7, 4, 11, 4, 2, 8, 3, 4, 3, 2, 4, -1, -1, -1, -1},
        {2, 9, 10, 2, 7, 9, 2, 3, 7, 7, 4, 9, -1, -1, -1, -1},
        {9, 10, 7, 9, 7, 4, 10, 2, 7, 8, 7, 0, 2, 0, 7, -1},
        {3, 7, 10, 3, 10, 2, 7, 4, 10, 1, 10, 0, 4, 0, 10, -1},
        {1, 10, 2, 8, 7, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {4, 9, 1, 4, 1, 7, 7, 1, 3, -1, -1, -1, -1, -1, -1, -1},
        {4, 9, 1, 4, 1, 7, 0, 8, 1, 8, 7, 1, -1, -1, -1, -1},
        {4, 0, 3, 7, 4, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {4, 8, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {9, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {3, 0, 9, 3, 9, 11, 11, 9, 10, -1, -1, -1, -1, -1, -1, -1},
        {0, 1, 10, 0, 10, 8, 8, 10, 11, -1, -1, -1, -1, -1, -1, -1},
        {3, 1, 10, 11, 3, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {1, 2, 11, 1, 11, 9, 9, 11, 8, -1, -1, -1, -1, -1, -1, -1},
        {3, 0, 9, 3, 9, 11, 1, 2, 9, 2, 11, 9, -1, -1, -1, -1},
        {0, 2, 11, 8, 0, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {3, 2, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {2, 3, 8, 2, 8, 10, 10, 8, 9, -1, -1, -1, -1, -1, -1, -1},
        {9, 10, 2, 0, 9, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {2, 3, 8, 2, 8, 10, 0, 1, 8, 1, 10, 8, -1, -1, -1, -1},
        {1, 10, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {1, 3, 8, 9, 1, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {0, 9, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {0, 3, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
        {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}

    };

}

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class MarchingCubes : MonoBehaviour
{
    MeshFilter meshFilter;
    MeshCollider meshCollider;

    List<Vector3> vertices = new();
    List<int> triangles = new();

    float[,,] terrainData;
    [SerializeField] int xLength = 20;
    [SerializeField] int yLength = 20;
    [SerializeField] int zLength = 20;
    [SerializeField] float terrainHeight = 0f;
    [SerializeField] bool smoothTerrain = true;

    int[,,,] vertexIndexArray;

    float t = 0f;
    float max, min;
    bool dostuff = false;

    [SerializeField] float valueDisplayFontSize = 1f;
    [SerializeField] GameObject DebugParent = null;
    [SerializeField] bool displayValues = true;
    [SerializeField] int displayPlane = 0;
    GameObject[,] valueDisplay;
    TextMeshPro[,] valueDisplayText;

    void Start()
    {
        vertexIndexArray = new int[xLength, yLength, zLength, 12];

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

        if (!TryGetComponent(out meshFilter))
        {
            Debug.Log("MeshFilter is NULL!");
            GetComponent<MarchingCubes>().enabled = false;
        }

        if (!TryGetComponent(out meshCollider)) 
        {
            Debug.Log("MeshCollider is NULL!");
            GetComponent<MarchingCubes>().enabled = false;
        }

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
                (max, min) = (min, max);
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
        float currentHeight;
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

                    noiseValue /= 6f;

                    currentHeight = yLength * noiseValue;
                    terrainData[x, y, z] = (float)y - currentHeight;
                    //terrainData[x, y, z] = noiseValue;

                }
            }
        }
    }


    void CreateMeshData()
    {
        //int[,,,] vertexIndexArray = new int[xLength, yLength, zLength, 12];
        for (int x = 0; x < xLength; x++)
        {
            for (int y = 0; y < yLength; y++)
            {
                for (int z = 0; z < zLength; z++)
                {
                    Vector3Int position = new(x, y, z);

                    float[] cube = new float[8];
                    for (int i = 0; i < 8; i++)
                    {
                        cube[i] = SampleTerrain(position + Tables.CornerTable[i]);
                    }

                    int configIndex = GetCubeCongif(cube);

                    if (configIndex == 0 || configIndex == 255) continue;

                    for (int TriangleVertexCounter = 0; TriangleVertexCounter < 15/*16*/; TriangleVertexCounter++)
                    {
                        int edgeIndex = Tables.TriangleTable[configIndex, TriangleVertexCounter];

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
                                break;
                        }
                    }
                }
            }
        }
        Debug.Log("triangle indices: " + triangles.Count);
    }

    int CalculateVertex(Vector3Int position, int index, float[] cube)
    {
        
        Vector3 vert1 = position + Tables.CornerTable[Tables.EdgeTable[index, 0]];
        Vector3 vert2 = position + Tables.CornerTable[Tables.EdgeTable[index, 1]];

        Vector3 vertPos;
        if (smoothTerrain)
        {
            float vert1Sample = cube[Tables.EdgeTable[index, 0]];
            float vert2Sample = cube[Tables.EdgeTable[index, 1]];

            float difference = vert2Sample - vert1Sample;

            if (difference == 0)
            {
                Debug.Log("DIFFERENCE IS 0");
            }

            difference = (terrainHeight - vert1Sample) / difference;

            vertPos = vert1 + (vert2 - vert1) * difference;
        }
        else
        {
            vertPos = (vert1 + vert2) / 2f;
        }

        vertices.Add(vertPos);
        int vertexCount = vertices.Count;
        triangles.Add(vertexCount - 1);

        return (vertexCount - 1);
    }

    int GetCubeCongif(float[] cube)
    {
        int configIndex = 0;
        for (int i = 0; i < 8; i++)
        {
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

    

    

}

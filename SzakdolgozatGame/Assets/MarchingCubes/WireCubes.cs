using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WireCubes : MonoBehaviour
{
    [SerializeField] GameObject Sphere0;
    [SerializeField] GameObject Sphere1;
    int x = 3;

    // Start is called before the first frame update
    void Start()
    {
        Sphere0.transform.position = transform.position;
        Sphere0.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
        Sphere1.transform.position = transform.position;
        Sphere1.transform.localScale = new Vector3(0.22f, 0.22f, 0.22f);
        StartCoroutine(DoStuff(0.5f));
    }

    // Update is called once per frame
    void Update()
    {

    }

    private void OnDrawGizmosSelected()
    {
        
        Gizmos.color = Color.blue;
        for (int i = 0; i < x; i++)
        {
            for (int j = 0; j < x; j++)
            {
                for (int k = 0; k < x; k++)
                {
                    Gizmos.DrawWireCube(new Vector3(i + 0.5f, j + 0.5f, k + 0.5f), new Vector3(1, 1, 1));
                }
            }
        }
        Gizmos.color = Color.green;
        for (int i = 0; i < x + 1; i++)
        {
            for (int j = 0; j < x + 1; j++)
            {
                for (int k = 0; k < x + 1; k++)
                {
                    Gizmos.DrawSphere(new Vector3(i, j, k), 0.1f);
                }
            }
        }
        
    }

    IEnumerator DoStuff(float w)
    {
        for (int i = 0; i < x + 1; i++)
        {
            for (int j = 0; j < x + 1; j++)
            {
                for (int k = 0; k < x + 1; k++)
                {
                    for (int c = 0; c < 12; c++)
                    {
                        
                        Sphere0.transform.position = new Vector3(i, j, k) + CornerTable[EdgeTable[c, 0]];
                        Sphere1.transform.position = new Vector3(i, j, k) + CornerTable[EdgeTable[c, 1]];
                        yield return new WaitForSeconds(5);
                    }
                }
            }
        }
    }

    Vector3Int[] CornerTable = new Vector3Int[8] {

        new Vector3Int(0, 0, 0),
        new Vector3Int(1, 0, 0),
        new Vector3Int(1, 1, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, 0, 1),
        new Vector3Int(1, 0, 1),
        new Vector3Int(1, 1, 1),
        new Vector3Int(0, 1, 1)

    };

    int[,] EdgeTable = new int[12, 2] {

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

    };
}

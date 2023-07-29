using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class Water : MonoBehaviour
{

    [SerializeField] private int lengthX = 10;
    [SerializeField] private int lengthZ = 10;
    [SerializeField] private int quadRes = 10;

    private Mesh mesh;
    private Vector3[] vertices;
    private Vector3[] normals;
    private void OnEnable()
    {
        CreatePlane();
    }

    //ref: https://catlikecoding.com/unity/tutorials/procedural-grid/
    private void CreatePlane()
    {
        GetComponent<MeshFilter>().mesh = mesh;

        int sideVertCountX = lengthX * quadRes;
        int sideVertCountZ = lengthZ * quadRes;
        vertices = new Vector3[(sideVertCountX + 1) * (sideVertCountZ + 1)];

        for(int z = 0, i = 0; z <= sideVertCountZ; z++)
        {
            for(int x = 0; x <= sideVertCountX; x++)
            {
                vertices[i] = new Vector3(x, 0, z);
                i++;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (vertices == null) return;
        for(int i = 0; i < vertices.Length; i++)
        {
            Gizmos.DrawSphere(vertices[i], 0.1f);
        }
    }
}

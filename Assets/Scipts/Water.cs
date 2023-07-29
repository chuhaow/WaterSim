using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static System.Runtime.InteropServices.Marshal;

[RequireComponent(typeof(MeshFilter))]
public class Water : MonoBehaviour
{
    [Serializable]
    public struct Wave
    {
        [Range(0.01f, 5.0f)]
        [SerializeField] private float amplitude;
        [Range(0.01f, 5.0f)]
        [SerializeField] private float frequency;

        public float CalulateSine(float x)
        {
            return amplitude * Mathf.Sin(x * frequency + Time.time);
        }
    }

    [SerializeField] private int lengthX = 10;
    [SerializeField] private int lengthZ = 10;
    [SerializeField] private int quadRes = 10;
    [SerializeField] private Wave[] waves;
    [SerializeField] private Shader waterShader;


    private Mesh mesh;
    private Vector3[] vertices;
    private Vector3[] normals;
    private ComputeBuffer waveBuffer;

    private Material waterMat;
    private void OnEnable()
    {
        CreatePlane();
        CreateMaterial();
        CreateBuffer();
    }

    private void CreateMaterial()
    {
        if (waterShader == null) return;
        if (waterMat != null) return;
        waterMat = new Material(waterShader);
        waterMat.SetInt("_WavesLength", waves.Length);
        MeshRenderer renderer = GetComponent<MeshRenderer>();

        renderer.material = waterMat;
    }

    private void CreateBuffer()
    {
        waveBuffer = new ComputeBuffer(4, SizeOf(typeof(Wave)));
        waterMat.SetBuffer("_Waves", waveBuffer);
    }

    //ref: https://catlikecoding.com/unity/tutorials/procedural-grid/
    private void CreatePlane()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        mesh.name = "procedural Plane";

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
        mesh.vertices = vertices;

        int[] triangles = new int[sideVertCountX * sideVertCountZ * 6];
        for(int tris = 0, vert = 0, z = 0; z < sideVertCountZ; z++, vert++)
        {
            for(int x = 0; x < sideVertCountX; x++, tris += 6, vert++)
            {
                triangles[tris] = vert;
                triangles[tris + 3] = triangles[tris + 2] = vert + 1;
                triangles[tris + 4] = triangles[tris + 1] = vert + sideVertCountX + 1;
                triangles[tris + 5] = vert + sideVertCountX + 2;
            }
        }

        mesh.triangles = triangles;
    }

    private void Update()
    {
        if (vertices == null) return;
        waveBuffer.SetData(waves);
        waterMat.SetBuffer("_Waves", waveBuffer);
        //for(int i = 0; i < vertices.Length; i++)
        //{
        //    Vector3 vert = transform.TransformPoint(vertices[i]);
        //    float height = SumWaves(vert.x * vert.z);
        //    vertices[i].y = height;
        //}
        //mesh.vertices = vertices;
    }

    private float SumWaves(float x)
    {
        float result = 0;
        for(int i = 0; i < waves.Length; i++)
        {
            result += waves[i].CalulateSine(x);
        }
        return result;
    }

    private void OnDrawGizmos()
    {
        //if (vertices == null) return;
        //for(int i = 0; i < vertices.Length; i++)
        //{
        //    Gizmos.DrawSphere(vertices[i], 0.1f);
        //}
    }
}

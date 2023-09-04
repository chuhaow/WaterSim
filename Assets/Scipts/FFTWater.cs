using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static System.Runtime.InteropServices.Marshal;
using UnityEngine.Rendering;
[RequireComponent(typeof(MeshFilter))]
public class FFTWater : MonoBehaviour
{
    [Serializable]
    public struct Wave
    {
        [Range(0.01f, 5.0f)]
        [SerializeField] private float amplitude;
        [Range(0.01f, 5.0f)]
        [SerializeField] private float frequency;
        [SerializeField] private Vector2 direction;
        [SerializeField] private float phase;
        [SerializeField] private float sharpness;

        public float CalulateSine(float x)
        {
            return amplitude * Mathf.Sin(x * frequency + Time.time);
        }
    }

    [Serializable]
    public struct FBMParam
    {
        [SerializeField] private float amplitude;
        [SerializeField] private float frequency;
        [SerializeField] private float speed;
        [SerializeField] private float seed;
        [SerializeField] private float sharpness;
        [SerializeField] private float ampMult;
        [SerializeField] private float frequencyMult;
        [SerializeField] private float speedMult;
        [SerializeField] private float seedIncrement;
        [SerializeField] private float sharpnessMult;
        [SerializeField] private int _FBMCount;

        public float Amplitude { get => amplitude; set => amplitude = value; }
        public float Frequency { get => frequency; set => frequency = value; }
        public float Speed { get => speed; set => speed = value; }
        public float Seed { get => seed; set => seed = value; }
        public float Sharpness { get => sharpness; set => sharpness = value; }
        public float AmpMult { get => ampMult; set => ampMult = value; }
        public float FrequencyMult { get => frequencyMult; set => frequencyMult = value; }
        public float SpeedMult { get => speedMult; set => speedMult = value; }
        public float SeedIncrement { get => seedIncrement; set => seedIncrement = value; }
        public float SharpnessMult { get => sharpnessMult; set => sharpnessMult = value; }
        public int FBMCount { get => _FBMCount; set => _FBMCount = value; }
    }



    [SerializeField] private int lengthX = 10;
    [SerializeField] private int lengthZ = 10;
    [SerializeField] private int quadRes = 10;
    [SerializeField] private Wave[] waves;
    [SerializeField] private Shader waterShader;
    [SerializeField] private ComputeShader cs;

    [SerializeField] private Color Ambient;
    [SerializeField] private Color Diffuse;
    [SerializeField] private Color Specular;
    [SerializeField] private float Reflectance;

    [SerializeField] private float Lacunarity;
    [Range(0.0f, 1.0f)]
    [SerializeField] private float Gain;
    private Mesh mesh;
    private Vector3[] vertices;
    private Vector3[] normals;
    private ComputeBuffer waveBuffer;

    private Material waterMat;

    private RenderTexture heightTex, normTex, spectrumTex, progSpectrumTex;

    [Header("FBM Param")]
    [SerializeField] private FBMParam WaveFBM;
    [SerializeField] private FBMParam NormFBM;


    private void OnEnable()
    {
        CreatePlane();
        CreateMaterial();
        CreateTextures();
        //CreateBuffer();

    }

    private void CreateTextures()
    {
        heightTex = new RenderTexture(512, 512, 0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
        heightTex.enableRandomWrite = true;
        heightTex.Create();

        normTex = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGB64, RenderTextureReadWrite.Linear);
        normTex.enableRandomWrite = true;
        normTex.Create();

        spectrumTex = new RenderTexture(512, 512, 0, RenderTextureFormat.ARGB64, RenderTextureReadWrite.Linear);
        spectrumTex.enableRandomWrite = true;
        spectrumTex.Create();

        progSpectrumTex = new RenderTexture(512, 512, 0, RenderTextureFormat.RGHalf, RenderTextureReadWrite.Linear);
        progSpectrumTex.enableRandomWrite = true;
        progSpectrumTex.Create();


        cs.SetTexture(0, "_HeightTex", heightTex);
        cs.SetTexture(0, "_NormalTex", normTex);
        cs.SetTexture(0, "_InitSpectrumTex", spectrumTex);
        cs.Dispatch(0, Mathf.CeilToInt(Screen.width / 8.0f), Mathf.CeilToInt(Screen.height / 8.0f), 1);

    }

    private void CreateMaterial()
    {
        if (waterShader == null) return;
        if (waterMat != null) return;
        waterMat = new Material(waterShader);
        //waterMat.SetInt("_WavesLength", waves.Length);

        MeshRenderer renderer = GetComponent<MeshRenderer>();

        renderer.material = waterMat;

    }

    private void CreateBuffer()
    {
        waveBuffer = new ComputeBuffer(waves.Length, SizeOf(typeof(Wave)));
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
        Vector2[] uv = new Vector2[vertices.Length];
        for (int z = 0, i = 0; z <= sideVertCountZ; z++)
        {
            for (int x = 0; x <= sideVertCountX; x++)
            {
                vertices[i] = new Vector3(x, 0, z);
                uv[i] = new Vector2((float)x / sideVertCountX, (float)z / sideVertCountZ);
                i++;
            }
        }
        mesh.vertices = vertices;
        mesh.uv = uv;
        int[] triangles = new int[sideVertCountX * sideVertCountZ * 6];
        for (int tris = 0, vert = 0, z = 0; z < sideVertCountZ; z++, vert++)
        {
            for (int x = 0; x < sideVertCountX; x++, tris += 6, vert++)
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

        cs.SetTexture(0, "_HeightTex", heightTex);
        cs.SetTexture(0, "_NormalTex", normTex);
        cs.SetTexture(0, "_InitSpectrumTex", spectrumTex);
        cs.SetFloat("_FrameTime", Time.time);

        cs.SetTexture(1, "_InitSpectrumTex", spectrumTex);
        cs.SetTexture(1, "_ProgSpectrumTex", progSpectrumTex);

        cs.Dispatch(1, Mathf.CeilToInt(Screen.width / 8.0f), Mathf.CeilToInt(Screen.height / 8.0f), 1);
        waterMat.SetTexture("_HeightTex", heightTex);
        waterMat.SetTexture("_NormalTex", normTex);
        waterMat.SetTexture("_SpectrumTex", progSpectrumTex);

        //for(int i = 0; i < vertices.Length; i++)
        //{
        //    Vector3 vert = transform.TransformPoint(vertices[i]);
        //    float height = SumWaves(vert.x * vert.z);
        //    vertices[i].y = height;
        //}
        //mesh.vertices = vertices;
    }


    private void OnDestroy()
    {
        //waveBuffer.Dispose();
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

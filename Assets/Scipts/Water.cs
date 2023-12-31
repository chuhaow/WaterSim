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

    [Header("FBM Param")]
    [SerializeField] private FBMParam WaveFBM;
    [SerializeField] private FBMParam NormFBM;

   
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
        //waveBuffer.SetData(waves);
        waterMat.SetBuffer("_Waves", waveBuffer);
        waterMat.SetColor("_Ambient", Ambient);
        waterMat.SetColor("_Diffuse", Diffuse);
        waterMat.SetColor("_Specular", Specular);
        waterMat.SetFloat("F0", Reflectance);
        waterMat.SetFloat("_BaseLacunarity", Lacunarity);
        waterMat.SetFloat("_BaseGain", Gain);

        waterMat.SetFloat("_WaveAmp", WaveFBM.Amplitude);
        waterMat.SetFloat("_WaveFreq", WaveFBM.Frequency);
        waterMat.SetFloat("_WaveSpeed", WaveFBM.Speed);
        waterMat.SetFloat("_WaveSeed", WaveFBM.Seed);
        waterMat.SetFloat("_WaveSharpness", WaveFBM.Sharpness);
        waterMat.SetFloat("_WaveAmpMult", WaveFBM.AmpMult);
        waterMat.SetFloat("_WaveFreqMult", WaveFBM.FrequencyMult);
        waterMat.SetFloat("_WaveSharpnessMult", WaveFBM.SharpnessMult);
        waterMat.SetFloat("_WaveSpeedMult", WaveFBM.SpeedMult);
        waterMat.SetFloat("_WaveSeedIncrement", WaveFBM.SeedIncrement);
        waterMat.SetFloat("_WaveCount", WaveFBM.FBMCount);

        waterMat.SetFloat("_NormAmp", NormFBM.Amplitude);
        waterMat.SetFloat("_NormFreq", NormFBM.Frequency);
        waterMat.SetFloat("_NormSpeed", NormFBM.Speed);
        waterMat.SetFloat("_NormSeed", NormFBM.Seed);
        waterMat.SetFloat("_NormSharpness", NormFBM.Sharpness);
        waterMat.SetFloat("_NormAmpMult", NormFBM.AmpMult);
        waterMat.SetFloat("_NormFreqMult", NormFBM.FrequencyMult);
        waterMat.SetFloat("_NormSharpnessMult", NormFBM.SharpnessMult);
        waterMat.SetFloat("_NormSpeedMult", NormFBM.SpeedMult);
        waterMat.SetFloat("_NormSeedIncrement", NormFBM.SeedIncrement);
        waterMat.SetFloat("_NormFBMCount", NormFBM.FBMCount);
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

    private void OnDestroy()
    {
        waveBuffer.Dispose();
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

Shader "Unlit/FFTWater"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"
            #include "UnityPBSLighting.cginc"
            #include "AutoLight.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                float3 worldPos: TEXCOORD1;
            };

            struct Wave
            {
                float amplitude;
                float frequency;
                float2 direction;
                float phase;
                float sharpness;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float F0;
            float _BaseGain;
            float _BaseLacunarity;
            StructuredBuffer<Wave> _Waves;
            int _WavesLength;

            float _WaveAmp, _WaveFreq, _WaveSpeed, _WaveSeed, _WaveSharpness, _WaveAmpMult, _WaveFreqMult, _WaveSharpnessMult, _WaveSpeedMult, _WaveSeedIncrement, _WaveCount;
            float _NormAmp, _NormFreq, _NormSpeed, _NormSeed, _NormSharpness, _NormAmpMult, _NormFreqMult, _NormSharpnessMult, _NormSpeedMult, _NormSeedIncrement, _NormFBMCount;

            float4 _Ambient, _Diffuse, _Specular;
            
            SamplerState linear_repeat_sampler;
            SamplerState point_repeat_sampler;
            Texture2D _HeightTex, _SpectrumTex;
            Texture2D _NormalTex;

            float Sine(float3 vert, Wave w) {
                float time = _Time.y * w.phase;
                float2 d = w.direction;
                float xz = d.x * vert.x + d.y * vert.z;
                return w.amplitude * sin(w.frequency * xz + time);
            }
            //https://catlikecoding.com/unity/tutorials/flow/waves/

            float3 FBM(float3 vert) {
                float freq = _WaveFreq;
                float amp = _WaveAmp;
                float speed = _WaveSpeed;
                float rnd = _WaveSeed;
                float ampSum = 0.0f;
                float sharpness = _WaveSharpness;
                float3 h = 0.0f;
                for (int i = 0; i < _WaveCount; i++) {
                    float2 dir = normalize(float2(cos(rnd), sin(rnd)));
                    float xz = dir.x * vert.x + dir.y * vert.z;
                    float3 wave = float3(0.0f, 0.0f, 0.0f);
                    wave.x = dir.x * sharpness * amp * cos(freq * xz + _Time.y * speed);
                    wave.z = dir.y * sharpness * amp * cos(freq * xz + _Time.y * speed);
                    wave.y = amp * sin(freq * xz + _Time.y * speed);
                    //float wave = amp * sin(freq * xz + _Time.y * speed);
                    ampSum += amp;
                    h += wave;
                    freq *= _WaveFreqMult;
                    amp *= _WaveAmpMult;
                    sharpness *= _WaveSharpnessMult;
                    speed *= _WaveSpeedMult;
                    rnd += _WaveSeedIncrement;
                }

                return h/ampSum;
            }

            float3 FBMNormal(float3 vert) {
                float freq = _NormFreq;
                float amp = _NormAmp;
                float speed = _NormSpeed;
                float rnd = _NormSeed;
                float ampSum = 0.0f;
                float sharpness = _NormSharpness;
                float3 n = 0.0f;
                for (int i = 0; i < _NormFBMCount; i++) {
                    float2 dir = normalize(float2(cos(rnd), sin(rnd)));
                    float xz = dir.x * vert.x + dir.y * vert.z;
                    float3 norm = float3(0.0f, 0.0f, 0.0f);

                    float wa = freq * amp;
                    float s = sin(freq * xz + _Time.y * speed);
                    float c = cos(freq * xz + _Time.y * speed);

                    norm.x = dir.x * wa * c;
                    norm.z = dir.y * wa * c;
                    norm.y = sharpness * wa * s;

                    ampSum += amp;
                    n += norm;
                    freq *= _NormFreqMult;
                    amp *= _NormAmpMult;
                    sharpness *= _NormSharpnessMult;
                    speed *= _NormSpeedMult;
                    rnd += _NormSeedIncrement;
                }

                return n / ampSum;
            }

            float3 Normal(float3 vert, Wave w) {
                float2 d = w.direction;
                float xz = d.x * vert.x + d.y * vert.z;
                float time = _Time.y * w.phase;
                float2 n = w.frequency * w.amplitude * d * cos(xz * w.frequency + time);

                return float3(n, 0.0f);
            }

            //Fresnel from https://developer.nvidia.com/gpugems/gpugems3/part-iii-rendering/chapter-14-advanced-techniques-realistic-real-time-skin
            float4 BlinnPhone(float3 p, float2 uv) {
                float3 lightDir = _WorldSpaceLightPos0;
                float3 E = normalize(_WorldSpaceCameraPos - p);
                float3 H = normalize(lightDir + E);
                float3 N = 0.0f;

                //N = FBMNormal(p);
                N = normalize(_NormalTex.Sample(linear_repeat_sampler, uv).rgb);
                float Kd = DotClamped(N, H);
                float4 diffuse = Kd * float4(_LightColor0.rgb, 1.0f) * _Diffuse;

                float base = 1 - dot(E, N);
                float exponential = pow(base, 25.0);
                float fresnel = exponential + F0 * (1.0 - exponential);

                float Ks = pow(DotClamped(N, H), 100.0);
                float4 spec = Ks * float4(_LightColor0.rgb, 1.0f) * _Specular * fresnel;

                float4 ambient = _Ambient;

             

                float4 color = saturate(ambient + diffuse + spec);
                return color;
            }

            v2f vert (appdata v)
            {

                v2f o;

                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.normal = normalize(UnityObjectToWorldNormal(v.normal));
                o.vertex = UnityObjectToClipPos(v.vertex + float3(0.0f, _HeightTex.SampleLevel(linear_repeat_sampler, v.uv , 0).r, 0.0f));
                //o.vertex =  UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                //return float4(0,0,0,1);
                //return float4(i.uv,0.0f,1.0f);
                //return normalize(_NormalTex.SampleLevel(linear_repeat_sampler, i.uv, 0));
                return BlinnPhone(i.worldPos, i.uv);
            }
            ENDCG
        }
    }
}

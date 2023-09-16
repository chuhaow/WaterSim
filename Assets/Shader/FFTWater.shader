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
            sampler2D _FoamTexture;
            float4 _MainTex_ST;
            float F0;
            float _BaseGain;
            float _BaseLacunarity;
            StructuredBuffer<Wave> _Waves;
            int _WavesLength;

            float _WaveAmp, _WaveFreq, _WaveSpeed, _WaveSeed, _WaveSharpness, _WaveAmpMult, _WaveFreqMult, _WaveSharpnessMult, _WaveSpeedMult, _WaveSeedIncrement, _WaveCount;
            float _NormAmp, _NormFreq, _NormSpeed, _NormSeed, _NormSharpness, _NormAmpMult, _NormFreqMult, _NormSharpnessMult, _NormSpeedMult, _NormSeedIncrement, _NormFBMCount;

            float4 _Ambient, _Diffuse, _Specular, _ScatterColour, _SunColour, _FresnelColour;
            float _BubbleDensity, _ScatterHeightMod;
            float _FresnelShine;
            float3 _FoamColour;

            SamplerState linear_repeat_sampler;
            SamplerState point_repeat_sampler;
            SamplerState trilinear_repeat_sampler;
            Texture2D _HeightTex, _SpectrumTex, _FoamTex;
            Texture2D _NormalTex;

            #define Tile 4

            float SmithMaskingBeckmann(float3 H, float3 S, float roughness) {
				float hdots = max(0.001f, DotClamped(H, S));
				float a = hdots / (roughness * sqrt(1 - hdots * hdots));
				float a2 = a * a;

				return a < 1.6f ? (1.0f - 1.259f * a + 0.396f * a2) / (3.535f * a + 2.181 * a2) : 0.0f;
			}

            
            float3 CalculateScatter(float3 lightDir, float3 viewDir ,float3 normal, float3 halfway,float2 tiledUV){
                float foam = _FoamTex.Sample(linear_repeat_sampler, tiledUV);
                
                float a = 0.075 + foam;//+ saturate(_HeightTex.SampleLevel(linear_repeat_sampler, uv, 0).y);
                float lightMask = SmithMaskingBeckmann(halfway, lightDir, a);


                float Hi = max(0.0f, _HeightTex.Sample(trilinear_repeat_sampler, tiledUV).y) * _ScatterHeightMod;
				float3 scatterColor = _ScatterColour;
				float3 bubbleColor = _Ambient;
				float bubbleDensity = _BubbleDensity;

				
				float k1 = 0.25f * Hi * pow(DotClamped(lightDir, -viewDir), 4.0f) * pow(0.5f - 0.5f * dot(lightDir, normal), 3.0f);
				float k2 = 1.0f * pow(DotClamped(viewDir, normal), 2.0f);
				float k3 = 0.5f * DotClamped(normal, lightDir);;
				float k4 = 1.0f * bubbleDensity;

				float3 scatter = (k1 + k2) * scatterColor * _SunColour * rcp(1 + lightMask);
				scatter += k3 * scatterColor * _SunColour + k4 * bubbleColor * _SunColour;
                return scatter;
            }

            //Fresnel from https://developer.nvidia.com/gpugems/gpugems3/part-iii-rendering/chapter-14-advanced-techniques-realistic-real-time-skin
            float4 BlinnPhone(float3 p, float2 uv) {
                float3 lightDir = normalize(_WorldSpaceLightPos0);
                float3 E = normalize(_WorldSpaceCameraPos - p);
                float3 H = normalize(lightDir + E);
               


                //N = FBMNormal(p);
                
                float3 N  = normalize( _NormalTex.Sample(linear_repeat_sampler, uv * Tile).rgb);
                float Kd = DotClamped(N, H);
                float4 diffuse = Kd * float4(_LightColor0.rgb, 1.0f) * _Diffuse;

                float base = 1 - dot(E, N);
                float exponential = pow(base, _FresnelShine);
                float fresnel = exponential + F0 * (1.0 - exponential);
                float3 fresnelColor = _FresnelColour * fresnel;

    //            float eta = 1.33f;
    //            float R = ((eta - 1) * (eta - 1)) / ((eta + 1) * (eta + 1));
    //            float numerator = pow(1 - dot(N, E), 5 * exp(-2.69 * 0.2f));
				//float fresnel = R + (1 - R) * numerator / (1.0f + 22.7f * pow(0.2f, 1.5f));
				//fresnel = saturate(fresnel);
		       

                float Ks = pow(DotClamped(N, H), 100.0);
                float4 spec = Ks * float4(_LightColor0.rgb, 1.0f) * _SunColour;

                float4 ambient = _Ambient;

                float foam = _FoamTex.Sample(linear_repeat_sampler, uv * Tile);

                float3 scatter = CalculateScatter(lightDir, E, N, H, uv * Tile);

                float3 colour = scatter + spec + fresnelColor;
                colour = (lerp(colour, _FoamColour, saturate(foam - 0.01f)));

                return float4(colour, 1.0f);
            }

            v2f vert (appdata v)
            {

                v2f o;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.normal = normalize(UnityObjectToWorldNormal(v.normal));
				float4 heightDisplacement = _HeightTex.SampleLevel(linear_repeat_sampler, v.uv * Tile + 0.01f, 0);
                v.vertex.xyz += mul(unity_WorldToObject, heightDisplacement.xyz);
                o.vertex = UnityObjectToClipPos(v.vertex);
                //i.pos = UnityObjectToClipPos(v.vertex);
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
                //return normalize(_NormalTex.SampleLevel(linear_repeat_sampler, i.uv * 8, 0));
                return BlinnPhone(i.worldPos, i.uv);
            }
            ENDCG
        }
    }
}

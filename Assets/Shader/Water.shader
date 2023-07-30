Shader "Unlit/Water"
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
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 worldPos: TEXCOORD1;
            };

            struct Wave
            {
                float amplitude;
                float frequency;
                float2 direction;
                float phase;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            StructuredBuffer<Wave> _Waves;
            int _WavesLength;

            float Sine(float3 vert, Wave w) {
                float time = _Time.y * w.phase;
                float xz = vert.x * vert.z;
                return w.amplitude * sin(w.frequency * xz + time);
            }

            float3 Normal(float3 vert, Wave w) {
                float2 d = w.direction;
                float xz = d.x * vert.x + d.y * vert.z;
                float time = _Time.y * w.phase;
                float2 n = w.frequency * w.amplitude * d * cos(xz * w.frequency + time);

                return float3(n, 0.0f);
            }

            float4 BlinnPhone(float3 p) {
                float3 lightDir = _WorldSpaceLightPos0;
                float3 E = normalize(_WorldSpaceCameraPos - p);
                float3 H = normalize(lightDir + E);
                float3 N = 0.0f;

                for(int i =0; i< _WavesLength;i++){
                    N += Normal(p, _Waves[i]);
                }
                N = normalize(N);
                float Kd = DotClamped(N, lightDir);
                float4 diffuse = Kd * float4(_LightColor0.rgb, 1.0f);

                float Ks = pow(DotClamped(N, H), 100.0);
                float4 spec = Ks * float4(_LightColor0.rgb, 1.0f);

                float4 ambient = float4( 0.0f, 0.0f, 0.1f, 0.0f);

                if (dot(lightDir, N) < 0.0) {
                    spec = float4(0, 0, 0, 1);
                }

                float4 color = saturate(ambient + diffuse + spec);
                return color;
            }

            v2f vert (appdata v)
            {
                v2f o;

                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                float height = 0.0f;
                for (int i = 0; i < _WavesLength; i++) {
                    height += Sine(o.worldPos, _Waves[i]);
                }
                o.vertex = UnityObjectToClipPos(v.vertex + float4(0.0f, height, 0.0f, 0.0f));

                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_MainTex, i.uv);
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return BlinnPhone(i.worldPos);
            }
            ENDCG
        }
    }
}

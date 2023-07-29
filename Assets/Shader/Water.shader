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
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            StructuredBuffer<Wave> _Waves;
            int _WavesLength;

            float Sine(float3 vert, Wave w) {
                float xz = vert.x * vert.z;
                return w.amplitude * sin(w.frequency * xz + _Time.y);
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
                return col;
            }
            ENDCG
        }
    }
}

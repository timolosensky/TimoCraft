Shader "Custom/VoxelShader"
{
    Properties
    {
        _MainTex ("Texture Array", 2DArray) = "white" {}
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
            #pragma require 2darray

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 uv : TEXCOORD0; // x,y = uv, z = texture index
            };

            struct v2f
            {
                float3 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD1;
            };

            UNITY_DECLARE_TEX2DARRAY(_MainTex);

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sampling aus dem Texture Array basierend auf dem Index (i.uv.z)
                fixed4 col = UNITY_SAMPLE_TEX2DARRAY(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}
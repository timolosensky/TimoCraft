Shader "Custom/VoxelShader"
{
    Properties
    {
        _MainTexArr ("Texture Array", 2DArray) = "" {}
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
            #pragma target 3.5 // Wichtig für Arrays

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 uv : TEXCOORD0; // 3D UVs! (u, v, index)
            };

            struct v2f
            {
                float3 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            UNITY_DECLARE_TEX2DARRAY(_MainTexArr);

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sampling: x,y sind Position, z ist der Index im Stapel
                return UNITY_SAMPLE_TEX2DARRAY(_MainTexArr, i.uv);
            }
            ENDCG
        }
    }
}
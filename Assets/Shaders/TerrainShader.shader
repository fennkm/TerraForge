Shader "Unlit/TerrainShader"
{
    Properties
    {
        _TerrainMasks ("Terrain Masks", 2DArray) = "texArr" {}
        _GrassTex ("Grass Texture", 2D) = "tex" {}
        _DesertTex ("Desert Texture", 2D) = "tex" {}
        _SnowTex ("Snow Texture", 2D) = "tex" {}
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

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            UNITY_DECLARE_TEX2DARRAY(_TerrainMasks);

            sampler2D _GrassTex;
            float4 _GrassTex_ST;
            sampler2D _DesertTex;
            float4 _DesertTex_ST;
            sampler2D _SnowTex;
            float4 _SnowTex_ST;

            uniform sampler2D terrainTex;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);

                o.uv = v.uv;
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col;

                fixed4 col1;
                col1.rgb = tex2D(_GrassTex, TRANSFORM_TEX(i.uv, _GrassTex)).rgb;
                col1.a = UNITY_SAMPLE_TEX2DARRAY(_TerrainMasks, float3(i.uv.xy, 0)).r;

                fixed4 col2;
                col2.rgb = tex2D(_DesertTex, TRANSFORM_TEX(i.uv, _DesertTex)).rgb;
                col2.a = UNITY_SAMPLE_TEX2DARRAY(_TerrainMasks, float3(i.uv.xy, 1)).r;

                fixed4 col3;
                col3.rgb = tex2D(_SnowTex, TRANSFORM_TEX(i.uv, _SnowTex)).rgb;
                col3.a = UNITY_SAMPLE_TEX2DARRAY(_TerrainMasks, float3(i.uv.xy, 2)).r;

                col = fixed4(
                    col1.rgb * col1.a +
                    col2.rgb * col2.a +
                    col3.rgb * col3.a, 1);

                return col;
            }
            ENDCG
        }
    }
}

Shader "Custom/TerrainShader"
{
    Properties
    {
        _PaintCursor ("Paint Cursor", int) = 1
        _InnerCursor ("Inner Cursor", 2D) = "tex" {}
        _OuterCursor ("Outer Cursor", 2D) = "tex" {}
        _InnerCursorScale ("Inner Cursor Scale", float) = 1
        _OuterCursorScale ("Outer Cursor Scale", float) = 1
        _CursorOffsetX ("Cursor Offset X", float) = 0
        _CursorOffsetY ("Cursor Offset Y", float) = 0
        _TerrainMasks ("Terrain Masks", 2DArray) = "texArr" {}
        _DesertTex ("Desert Texture", 2D) = "tex" {}
        _GrassTex ("Grass Texture", 2D) = "tex" {}
        _SnowTex ("Snow Texture", 2D) = "tex" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0
        
        UNITY_DECLARE_TEX2DARRAY(_TerrainMasks);

        int _PaintCursor;

        sampler2D _InnerCursor;
        sampler2D _OuterCursor;
        float _InnerCursorScale;
        float _OuterCursorScale;
        float _CursorOffsetX;
        float _CursorOffsetY;
        
        sampler2D _DesertTex;
        float4 _DesertTex_ST;
        sampler2D _GrassTex;
        float4 _GrassTex_ST;
        sampler2D _SnowTex;
        float4 _SnowTex_ST;

        struct Input
        {
            float2 uv_TerrainMasks;
            float2 uv_UILayer;
        };

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 col1;
            col1.rgb = tex2D(_DesertTex, TRANSFORM_TEX(IN.uv_TerrainMasks, _DesertTex)).rgb;
            col1.a = UNITY_SAMPLE_TEX2DARRAY(_TerrainMasks, float3(IN.uv_TerrainMasks.xy, 0)).r;

            fixed4 col2;
            col2.rgb = tex2D(_GrassTex, TRANSFORM_TEX(IN.uv_TerrainMasks, _GrassTex)).rgb;
            col2.a = UNITY_SAMPLE_TEX2DARRAY(_TerrainMasks, float3(IN.uv_TerrainMasks.xy, 1)).r;

            fixed4 col3;
            col3.rgb = tex2D(_SnowTex, TRANSFORM_TEX(IN.uv_TerrainMasks, _SnowTex)).rgb;
            col3.a = UNITY_SAMPLE_TEX2DARRAY(_TerrainMasks, float3(IN.uv_TerrainMasks.xy, 2)).r;

            fixed3 col = fixed3(
                col1.rgb * col1.a +
                col2.rgb * col2.a +
                col3.rgb * col3.a);

            if (_PaintCursor)
            {
                fixed4 outerCursorCol;
                fixed4 innerCursorCol;

                float2 outerCursorUV = float2(0.5, 0.5) +
                    (IN.uv_TerrainMasks - float2(_CursorOffsetX, _CursorOffsetY)) / _OuterCursorScale;

                float2 innerCursorUV = float2(0.5, 0.5) + 
                    (IN.uv_TerrainMasks - float2(_CursorOffsetX, _CursorOffsetY)) / _InnerCursorScale;

                outerCursorCol = tex2D(_OuterCursor, outerCursorUV);
                innerCursorCol = tex2D(_InnerCursor, innerCursorUV);

                col += outerCursorCol.rbg * outerCursorCol.a + innerCursorCol.rbg * innerCursorCol.a;
            }

            o.Albedo = col;
            o.Alpha = 1.0;
            o.Metallic = 0.0;
            o.Smoothness = 0.0;
        }
        ENDCG
    }
    FallBack "Diffuse"
}

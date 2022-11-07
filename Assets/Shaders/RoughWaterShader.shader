Shader "Custom/RoughWaterShader"
{
    Properties
    {

        _MainTex("Texture", 2D) = "tex" {}
        _Resolution("Terrain Size", float) = 129

        _ScrollSpeedX1("X speed 1", Range(-10, 10)) = 2
        _ScrollSpeedY1("Y speed 1", Range(-10, 10)) = 2
        _NormalTex1("Bump 1", 2D) = "bump" {}
        _NormalIntensity1("Normal Intensity 1", Range(0, 5)) = 1
        _ScrollSpeedX2("X speed 2", Range(-10, 10)) = 2
        _ScrollSpeedY2("Y speed 2", Range(-10, 10)) = 2
        _NormalTex2("Bump 2", 2D) = "bump" {}
        _NormalIntensity2("Normal Intensity 2", Range(0, 5)) = 1
        
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        ZWrite Off
        LOD 100

        CGPROGRAM
        // Physically based Standard lighting model, and enable shadows on all light types
        #pragma surface surf Standard fullforwardshadows alpha:fade

        // Use shader model 3.0 target, to get nicer looking lighting
        #pragma target 3.0

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NormalTex1;
            float2 uv_NormalTex2;
        };

        sampler2D _MainTex;

        sampler2D _NormalTex1;
        sampler2D _NormalTex2;

        float _Resolution;

        fixed4 _ShallowColour;
        fixed4 _DeepColour;

        float _NormalIntensity1;
        fixed _ScrollSpeedX1;
        fixed _ScrollSpeedY1;

        float _NormalIntensity2;
        fixed _ScrollSpeedX2;
        fixed _ScrollSpeedY2;

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float Pi = 6.28318530718; // Pi*2
    
            // GAUSSIAN BLUR SETTINGS {{{
            float Directions = 16.0; // BLUR DIRECTIONS (Default 16.0 - More is better but slower)
            float Quality = 3.0; // BLUR QUALITY (Default 4.0 - More is better but slower)
            float Size = 8.0; // BLUR SIZE (Radius)
            // GAUSSIAN BLUR SETTINGS }}}

            float2 uv = IN.uv_MainTex / _Resolution + fixed2(.5, .5);
            fixed4 col = tex2D(_MainTex, uv);
        
            float radius = Size/_Resolution;
            
            // Blur calculations
            for( float d=0.0; d<Pi; d+=Pi/Directions)
            {
                for(float i=1.0/Quality; i<=1.0; i+=1.0/Quality)
                {
                    col += tex2D( _MainTex, uv + float2(cos(d), sin(d)) * radius * i);		
                }
            }
            
            // Output to screen
            col /= Quality * Directions - 15.0;
            
            o.Albedo = col.rgb;
            o.Alpha = col.a;

            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;

            // normal map scrolling part
            float2 uvScrollNormal1 = IN.uv_NormalTex1;
            uvScrollNormal1 += fixed2(_ScrollSpeedX1 * _Time.y, _ScrollSpeedY1 * _Time.y);
            float3 normalMap1 = UnpackNormal(tex2D(_NormalTex1, uvScrollNormal1));
            
            float2 uvScrollNormal2 = IN.uv_NormalTex2;
            uvScrollNormal2 += fixed2(_ScrollSpeedX2 * _Time.y, _ScrollSpeedY2 * _Time.y);
            float3 normalMap2 = UnpackNormal(tex2D(_NormalTex2, uvScrollNormal2));

            // normal map intensity part 
            normalMap1 *= _NormalIntensity1;
            normalMap2 *= _NormalIntensity2;
            o.Normal = normalize(normalMap1.rgb + normalMap2.rgb);
        }
        ENDCG
    }
    FallBack "Diffuse"
}

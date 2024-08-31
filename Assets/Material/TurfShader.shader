Shader "Custom/NonReflectiveGrassShader"
{
    Properties
    {
        _MainTex ("Grass Texture", 2D) = "white" {}
        _DetailTex ("Detail Texture", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _Color ("Tint Color", Color) = (1, 1, 1, 1)
        _MainTexTiling ("Main Texture Tiling", Vector) = (1, 1, 0, 0)
        _DetailTexTiling ("Detail Texture Tiling", Vector) = (1, 1, 0, 0)
        _WindStrength ("Wind Strength", Range(0, 1)) = 0.5
        _WindSpeed ("Wind Speed", Range(0, 5)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Lambert vertex:vert addshadow
        #pragma target 3.0

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_DetailTex;
        };

        sampler2D _MainTex;
        sampler2D _DetailTex;
        sampler2D _NormalMap;
        fixed4 _Color;
        float4 _MainTexTiling;
        float4 _DetailTexTiling;
        float _WindStrength;
        float _WindSpeed;

        void vert (inout appdata_full v)
        {
            // Apply wind effect to vertex position
            float3 wind = sin(_Time.y * _WindSpeed + v.vertex.x * 10.0) * _WindStrength;
            v.vertex.xyz += wind * v.normal * 0.1; // Adjust the multiplier for wind effect strength
        }

        void surf (Input IN, inout SurfaceOutput o)
        {
            // Sample the main grass texture with its tiling value
            fixed4 mainTexColor = tex2D(_MainTex, IN.uv_MainTex * _MainTexTiling.xy) * _Color;

            // Sample the detail texture with its own tiling value
            fixed4 detailTexColor = tex2D(_DetailTex, IN.uv_DetailTex * _DetailTexTiling.xy);
            mainTexColor.rgb += detailTexColor.rgb * 0.5;

            // Sample the normal map with the main texture tiling value
            fixed3 normalMap = UnpackNormal(tex2D(_NormalMap, IN.uv_MainTex * _MainTexTiling.xy));
            o.Normal = normalMap;

            // Set Albedo and Alpha
            o.Albedo = mainTexColor.rgb; // Use the main texture color
            o.Alpha = 1.0; // Set alpha to 1 for opaque material

            // Set specular and smoothness to zero for a matte finish
            o.Specular = 0.0; // No specular highlights
            o.Gloss = 0.0; // No glossiness
        }
        ENDCG
    }
    FallBack "Diffuse"
}

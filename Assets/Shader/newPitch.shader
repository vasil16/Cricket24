Shader "Custom/newPitch" {
    Properties {
        _Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Base (RGB) Trans (A)", 2D) = "white" {}
        _EdgeBlendColor ("Edge Blend Color", Color) = (0,0,0,0)
        _EdgeBlendPower ("Edge Blend Power", Range(0, 5)) = 1
        _EdgeWidth ("Edge Width", Range(0, 1)) = 0.1
        _Glossiness ("Smoothness", Range(0,1)) = 0.5
        _Metallic ("Metallic", Range(0,1)) = 0.0
    }

    SubShader {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        LOD 300

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows alpha:fade
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        fixed4 _EdgeBlendColor;
        float _EdgeBlendPower;
        float _EdgeWidth;
        half _Glossiness;
        half _Metallic;

        struct Input {
            float2 uv_MainTex;
            float3 worldPos;
            float3 worldNormal;
            INTERNAL_DATA
        };

        void surf (Input IN, inout SurfaceOutputStandard o) {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            
            // Improved edge blending
            float3 worldViewDir = normalize(UnityWorldSpaceViewDir(IN.worldPos));
            float3 worldNormal = WorldNormalVector(IN, o.Normal);
            float fresnel = 1.0 - saturate(dot(worldViewDir, worldNormal));
            fresnel = smoothstep(_EdgeWidth, 1.0, pow(fresnel, _EdgeBlendPower));
            
            o.Albedo = lerp(o.Albedo, _EdgeBlendColor.rgb, fresnel * _EdgeBlendColor.a);
            o.Alpha = lerp(c.a * _Color.a, _EdgeBlendColor.a, fresnel);
            
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }
        ENDCG
    }
    FallBack "Transparent/VertexLit"
}
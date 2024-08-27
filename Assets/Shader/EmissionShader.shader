Shader "Custom/AnimatedEmissiveText"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _EmissionMap ("Emission Map", 2D) = "black" {}
        [HDR] _EmissionColor ("Emission Color", Color) = (1,1,1,1)
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 1
        _PulseMinIntensity ("Min Intensity", Range(0, 1)) = 0.5
        _PulseMaxIntensity ("Max Intensity", Range(1, 5)) = 2
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows

        sampler2D _MainTex;
        sampler2D _EmissionMap;
        half4 _EmissionColor;
        float _PulseSpeed;
        float _PulseMinIntensity;
        float _PulseMaxIntensity;

        struct Input
        {
            float2 uv_MainTex;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            // Base color
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex);
            o.Albedo = c.rgb;
            
            // Animated emission
            float pulseIntensity = lerp(_PulseMinIntensity, _PulseMaxIntensity, 
                (sin(_Time.y * _PulseSpeed) + 1) * 0.5);
                
            fixed4 e = tex2D(_EmissionMap, IN.uv_MainTex);
            o.Emission = e.rgb * _EmissionColor.rgb * pulseIntensity;
            
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
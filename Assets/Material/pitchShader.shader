Shader "Custom/PitchShader"
{
    Properties
    {
        _MainTex ("Grass Texture", 2D) = "white" {} // Main grass texture
        _Color ("Tint Color", Color) = (1, 1, 1, 1) // Tint color for the grass
        _Tiling ("Tiling", Vector) = (1, 1, 0, 0) // Tiling for the texture
        _EdgeFade ("Edge Fade", Range(0, 1)) = 0.1 // Edge fade amount
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" }
        LOD 200

        Blend SrcAlpha OneMinusSrcAlpha // Enable alpha blending
        ZWrite Off // Disable depth writing

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float edgeDistance : TEXCOORD1; // Distance from the edge
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _Tiling;
            float _EdgeFade;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv * _Tiling.xy; // Apply tiling

                // Calculate edge distance based on UV coordinates
                float edgeDistX = min(o.uv.x, 1.0 - o.uv.x);
                float edgeDistY = min(o.uv.y, 1.0 - o.uv.y);
                o.edgeDistance = min(edgeDistX, edgeDistY); // Get the minimum distance to the edge

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the grass texture
                fixed4 texColor = tex2D(_MainTex, i.uv) * _Color; // Combine texture color with tint

                // Apply edge fading
                float alpha = smoothstep(0.0, _EdgeFade, i.edgeDistance); // Smooth transition based on edge distance
                texColor.a *= alpha; // Apply the alpha to the texture color

                return texColor;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
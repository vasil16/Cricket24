Shader "Custom/PitchShader"
{
    Properties
    {
        _MainTex ("Grass Texture", 2D) = "white" {} // Main grass texture
        _Color ("Tint Color", Color) = (1, 1, 1, 1) // Tint color for the grass
        _Tiling ("Tiling", Vector) = (1, 1, 0, 0) // Tiling for the texture
        _EdgeFadeLR ("Left/Right Edge Fade", Range(0, 1)) = 0.1 // Edge fade amount for left and right
        _EdgeFadeTB ("Top/Bottom Edge Fade", Range(0, 1)) = 0.1 // Edge fade amount for top and bottom
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" } // Keep transparency
        LOD 200

        Blend SrcAlpha OneMinusSrcAlpha // Enable alpha blending
        ZWrite Off // Disable depth writing for transparency

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
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 normalWS : TEXCOORD1; // World space normal
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _Tiling;
            float _EdgeFadeLR; // Left/Right edge fade amount
            float _EdgeFadeTB; // Top/Bottom edge fade amount

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv * _Tiling.xy; // Apply tiling
                o.normalWS = normalize(mul(v.normal, (float3x3)unity_ObjectToWorld)); // Transform normal to world space

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Sample the grass texture
                fixed4 texColor = tex2D(_MainTex, i.uv) * _Color; // Combine texture color with tint

                // Simple Lambert lighting
                half3 lightDir = normalize(_WorldSpaceLightPos0.xyz); // Get the main directional light direction
                half lambert = saturate(dot(i.normalWS, lightDir)); // Lambert's cosine law

                // Calculate alpha based on edge distances and corresponding edge fade values
                float alpha = 1.0; // Start with full alpha

                // Apply left and right edge fading
                if (i.uv.x < _EdgeFadeLR) // Left edge
                {
                    alpha *= smoothstep(0.0, _EdgeFadeLR, i.uv.x);
                }
                if (i.uv.x > 1.0 - _EdgeFadeLR) // Right edge
                {
                    alpha *= smoothstep(0.0, _EdgeFadeLR, 1.0 - i.uv.x);
                }

                // Apply top and bottom edge fading
                if (i.uv.y < _EdgeFadeTB) // Bottom edge
                {
                    alpha *= smoothstep(0.0, _EdgeFadeTB, i.uv.y);
                }
                if (i.uv.y > 1.0 - _EdgeFadeTB) // Top edge
                {
                    alpha *= smoothstep(0.0, _EdgeFadeTB, 1.0 - i.uv.y);
                }

                texColor.a *= alpha; // Apply the calculated alpha to the texture color

                // Combine the texture color and lighting (no self-lighting)
                texColor.rgb *= lambert;

                return texColor;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}

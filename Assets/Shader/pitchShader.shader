Shader "Custom/GrassEdgeFadeShader"
{
    Properties
    {
        _MainTex ("Grass Texture", 2D) = "white" {} // Main grass texture
        _Color ("Tint Color", Color) = (1, 1, 1, 1) // Tint color for the grass
        _EdgeFadeLR ("Left/Right Edge Fade", Range(0, 1)) = 0.1 // Edge fade amount for left and right
        _EdgeFadeTB ("Top/Bottom Edge Fade", Range(0, 1)) = 0.1 // Edge fade amount for top and bottom
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType"="Transparent" }
        LOD 200

        Pass
        {
            Tags { "LightMode"="ForwardBase" }
            Blend SrcAlpha OneMinusSrcAlpha // Enable alpha blending for transparency
            ZWrite On // Enable depth writing
            Cull Off // Disable face culling to show both sides

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
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _Color;
            float _EdgeFadeLR;
            float _EdgeFadeTB;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Sample the grass texture
                fixed4 texColor = tex2D(_MainTex, i.uv) * _Color;

                // Edge fading calculations
                float alpha = 1.0; // Default alpha value

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

                return texColor;
            }
            ENDCG
        }
    }

    FallBack "Transparent/Cutout/Diffuse"
}

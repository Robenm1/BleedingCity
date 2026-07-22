Shader "Custom/LightningLine_Additive"
{
    Properties
    {
        _Color ("Lightning Color", Color) = (1, 0.95, 0.25, 1)
        _CoreColor ("Core Color", Color) = (1, 1, 1, 1)

        _Brightness ("Brightness", Range(0, 10)) = 4
        _CoreStrength ("Core Strength", Range(0, 5)) = 2

        _NoiseScale ("Noise Scale", Range(1, 80)) = 25
        _NoiseSpeed ("Noise Speed", Range(0, 20)) = 8
        _FlickerStrength ("Flicker Strength", Range(0, 1)) = 0.35

        _EdgeFade ("Edge Fade", Range(0.01, 1)) = 0.25
        _AlphaCut ("Alpha Cut", Range(0, 1)) = 0.2
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "IgnoreProjector"="True"
        }

        Blend SrcAlpha One
        ZWrite Off
        Cull Off
        Lighting Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            fixed4 _Color;
            fixed4 _CoreColor;

            float _Brightness;
            float _CoreStrength;

            float _NoiseScale;
            float _NoiseSpeed;
            float _FlickerStrength;

            float _EdgeFade;
            float _AlphaCut;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            float Hash(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float Noise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);

                float a = Hash(i);
                float b = Hash(i + float2(1, 0));
                float c = Hash(i + float2(0, 1));
                float d = Hash(i + float2(1, 1));

                float2 u = f * f * (3.0 - 2.0 * f);

                return lerp(a, b, u.x)
                    + (c - a) * u.y * (1.0 - u.x)
                    + (d - b) * u.x * u.y;
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float time = _Time.y * _NoiseSpeed;

                // LineRenderer UV:
                // x = along the line
                // y = across the width
                float along = i.uv.x;
                float across = abs(i.uv.y - 0.5) * 2.0;

                // Fade edges so the middle is hotter/brighter.
                float edge = 1.0 - smoothstep(1.0 - _EdgeFade, 1.0, across);

                // Procedural electric noise.
                float n1 = Noise(float2(along * _NoiseScale + time, across * 8.0));
                float n2 = Noise(float2(along * _NoiseScale * 0.5 - time * 1.7, across * 16.0));

                float lightning = saturate((n1 + n2) * 0.7);

                // Broken lightning flicker.
                float broken = step(_AlphaCut, lightning);

                // Fast full-line flicker.
                float flicker = 1.0 - (Hash(float2(floor(_Time.y * 40.0), 7.13)) * _FlickerStrength);

                // White hot core.
                float core = 1.0 - smoothstep(0.0, 0.45, across);
                core *= _CoreStrength;

                float alpha = edge * broken * flicker * i.color.a * _Color.a;

                fixed3 col = _Color.rgb * _Brightness;
                col += _CoreColor.rgb * core;

                return fixed4(col * i.color.rgb, alpha);
            }
            ENDCG
        }
    }
}
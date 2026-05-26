Shader "Custom/FlashlightOverlay"
{
    Properties
    {
        _MainTex     ("Texture (unused)", 2D)       = "white" {}
        _Color       ("Dark Color", Color)           = (0, 0, 0, 0.82)
        _MousePos    ("Mouse UV",   Vector)          = (0.5, 0.5, 0, 0)
        _Radius      ("Light Radius",   Float)       = 0.18
        _Softness    ("Edge Softness",  Float)       = 0.07
        _AspectRatio ("Aspect Ratio",   Float)       = 1.78
    }

    SubShader
    {
        Tags
        {
            "Queue"           = "Overlay"
            "IgnoreProjector" = "True"
            "RenderType"      = "Transparent"
        }

        Cull     Off
        Lighting Off
        ZWrite   Off
        ZTest    Always
        Blend    SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            float4 _Color;
            float4 _MousePos;
            float  _Radius;
            float  _Softness;
            float  _AspectRatio;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 diff = IN.uv - _MousePos.xy;
                diff.x *= _AspectRatio;

                float dist  = length(diff);

                // smoothstep: 0 inside the light, 1 in full darkness
                float alpha = smoothstep(_Radius, _Radius + _Softness, dist);
                return half4(_Color.rgb, alpha * _Color.a);
            }
            ENDHLSL
        }
    }
}

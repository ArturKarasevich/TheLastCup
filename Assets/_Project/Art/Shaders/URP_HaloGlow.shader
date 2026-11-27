Shader "TheLastCup/URP_HaloGlow"
{
    Properties
    {
        [HDR] _HaloColor ("Halo Color", Color) = (2.5, 2.5, 2.5, 1)
        _Falloff ("Falloff", Range(1, 6)) = 2.1
        _PulseSpeed ("Pulse Speed", Float) = 5.0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent+200"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "HaloGlow"
            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha One

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _HaloColor;
                half _Falloff;
                half _PulseSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 p = IN.uv * 2.0 - 1.0;
                float d = length(p);
                half glow = pow(saturate(1.0h - (half)d), _Falloff);
                half pulse = lerp(0.45h, 1.0h, (sin(_Time.y * _PulseSpeed) + 1.0h) * 0.5h);
                return half4(_HaloColor.rgb * glow, _HaloColor.a * glow * pulse);
            }
            ENDHLSL
        }
    }
}

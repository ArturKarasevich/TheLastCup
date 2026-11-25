Shader "Custom/URP_StencilOutline"
{
    Properties
    {
        _OutlineColor ("Color", Color) = (1, 1, 1, 1)
        _Thickness ("Thickness", Range(0.001, 0.05)) = 0.01
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Transparent+500" 
        }

        Pass
        {
            Name "StencilOutline"
            Cull Off
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            Stencil
            {
                Ref 1
                Comp NotEqual
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _Thickness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float3 n = TransformObjectToWorldNormal(input.normalOS);
                float lenSq = dot(n, n);
                float3 normalWS = lenSq > 1e-6 ? n * rsqrt(lenSq) : float3(0, 1, 0);

                worldPos += normalWS * _Thickness;
                output.positionCS = TransformWorldToHClip(worldPos);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
}

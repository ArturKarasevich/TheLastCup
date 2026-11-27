Shader "Custom/URP_RimHighlight"
{
    Properties
    {
        [HDR] _RimColor ("Rim Color", Color) = (1, 1, 1, 1)
        [HDR] _OutlineColor ("Outline Color", Color) = (1, 1, 1, 1)
        _RimPower ("Rim Sharpness", Range(0.5, 10.0)) = 2.8
        _RimIntensity ("Rim Intensity", Range(0.1, 5.0)) = 1.6
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Transparent+100" 
        }

        Pass
        {
            Name "RimPass"
            Blend One One          // Аддитивное смешивание: только добавляет свет по краям
            ZWrite Off
            ZTest LEqual
            Cull Back

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
                float3 normalWS   : TEXCOORD0;
                float3 viewDirWS  : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _RimColor;
                float4 _OutlineColor;
                float _RimPower;
                float _RimIntensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.normalWS = normInputs.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normal = normalize(input.normalWS);
                float3 viewDir = normalize(input.viewDirWS);

                // Эффект Френеля: 0 в центре взгляда, 1 на касательных краях силуэта
                float NdotV = saturate(dot(normal, viewDir));
                float rim = 1.0 - NdotV;
                rim = pow(rim, _RimPower) * _RimIntensity;

                return _RimColor * _OutlineColor * _OutlineColor.a * rim;
            }
            ENDHLSL
        }
    }
}

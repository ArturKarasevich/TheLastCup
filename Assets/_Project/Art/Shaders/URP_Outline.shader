Shader "Custom/URP_Outline"
{
    Properties
    {
        _OutlineColor ("Color", Color) = (1, 1, 1, 1)
        _Thickness ("Overall Thickness", Range(0.001, 0.05)) = 0.015
        _ThicknessX ("Thickness X (Width)", Range(0.1, 2.0)) = 0.6
        _ThicknessY ("Thickness Y (Height)", Range(0.1, 2.0)) = 0.5
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Transparent+10"
        }

        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite Off
            ZTest Greater
            Blend SrcAlpha OneMinusSrcAlpha

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
                float _ThicknessX;
                float _ThicknessY;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                // Переводим вершину и нормаль в Clip Space для идеальной толщины на экране
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(input.normalOS);

                float4 posCS = posInputs.positionCS;
                float3 normCS = mul((float3x3)UNITY_MATRIX_VP, normInputs.normalWS);

                // Нормализуем направление с учетом пропорций и масштабирования осей
                float2 offset = normalize(normCS.xy);
                offset.x *= _ThicknessX;
                offset.y *= _ThicknessY;

                posCS.xy += offset * _Thickness * posCS.w;
                output.positionCS = posCS;

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

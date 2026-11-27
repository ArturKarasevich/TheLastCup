Shader "TheLastCup/URP_Glass"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (0.78, 0.80, 0.82, 0.08)
        _Smoothness ("Smoothness", Range(0, 1)) = 0.92
        _FresnelPower ("Fresnel Power", Range(1, 6)) = 3.2
        _FresnelStrength ("Fresnel Strength", Range(0, 1)) = 0.42
        _DustStrength ("Aged Dust Strength", Range(0, 1)) = 0.03
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
        }

        Pass
        {
            Name "GlassForward"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite Off
            ZTest LEqual
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _FORWARD_PLUS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half _Smoothness;
                half _FresnelPower;
                half _FresnelStrength;
                half _DustStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float3 viewDirWS  : TEXCOORD3;
                float3 posOS      : TEXCOORD4;
            };

            Varyings Vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionCS = posInputs.positionCS;
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = normInputs.normalWS;
                OUT.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);
                OUT.posOS = IN.positionOS.xyz;
                return OUT;
            }

            half CalcSpec(float3 N, float3 V, float3 L, half specPower)
            {
                float3 H = normalize(L + V);
                return pow(saturate(dot(N, H)), specPower);
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(IN.viewDirWS);
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);

                half n1 = sin(IN.posOS.x * 28.0 + cos(IN.posOS.y * 22.0)) * 0.5h + 0.5h;
                half n2 = cos(IN.posOS.z * 35.0 - sin(IN.posOS.y * 19.0)) * 0.5h + 0.5h;
                half dust = n1 * n2 * _DustStrength;

                half smoothVal = saturate(_Smoothness - dust);
                half specPower = lerp(24.0h, 256.0h, smoothVal);

                Light mainLight = GetMainLight();
                half3 totalSpec = mainLight.color * CalcSpec(N, V, normalize(mainLight.direction), specPower) * mainLight.distanceAttenuation;

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = N;
                inputData.viewDirectionWS = V;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);

                uint pixelLightCount = GetAdditionalLightsCount();
                LIGHT_LOOP_BEGIN(pixelLightCount)
                    Light addLight = GetAdditionalLight(lightIndex, inputData.positionWS);
                    totalSpec += addLight.color * CalcSpec(N, V, normalize(addLight.direction), specPower) * addLight.distanceAttenuation;
                LIGHT_LOOP_END

                half3 reflDir = reflect(-V, N);
                half3 envRefl = GlossyEnvironmentReflection(reflDir, 1.0h - smoothVal, 1.0h);

                half fresnel = pow(1.0h - saturate(dot(N, V)), _FresnelPower);
                half3 baseCol = _BaseColor.rgb * tex.rgb;
                half3 col = baseCol * 0.75h
                          + envRefl * (0.40h + fresnel * 0.85h)
                          + totalSpec * 1.8h;

                half specLum = max(totalSpec.r, max(totalSpec.g, totalSpec.b));
                half alpha = saturate(_BaseColor.a * tex.a + dust * 0.2h + fresnel * _FresnelStrength + specLum * 0.85h);
                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}

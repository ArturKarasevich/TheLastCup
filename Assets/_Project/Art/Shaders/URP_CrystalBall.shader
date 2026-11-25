Shader "TheLastCup/URP_CrystalBall"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _GlassAlpha ("Sphere Glass Alpha", Range(0.05, 0.8)) = 0.26
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent-5"
            "RenderType" = "Transparent"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _GlassAlpha;
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
            float3 normalWS   : TEXCOORD1;
            float3 viewDirWS  : TEXCOORD2;
        };

        Varyings Vert(Attributes IN)
        {
            Varyings OUT;
            VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
            VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS);

            OUT.positionCS = posInputs.positionCS;
            OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
            OUT.normalWS = normInputs.normalWS;
            OUT.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);
            return OUT;
        }

        // In GreyBox_MagicSphere_base.png:
        // Left island (uv.x <= 0.32) and Right islands (uv.x >= 0.728) are the Pedestal/Base.
        // Middle two circular islands (0.32 < uv.x < 0.728) are the Sphere dome.
        bool IsSphereUV(float2 uv)
        {
            return (uv.x > 0.32 && uv.x < 0.728);
        }

        half3 ApplySimpleLit(half3 albedo, float3 normalWS)
        {
            Light mainLight = GetMainLight();
            float3 N = normalize(normalWS);
            half NdotL = saturate(dot(N, mainLight.direction)) * 0.65h + 0.35h;
            half3 ambient = SampleSH(N);
            return albedo * (mainLight.color * NdotL + ambient * 0.55h);
        }
        ENDHLSL

        // PASS 1: Opaque Base / Pedestal (keeps exact original texture & color)
        Pass
        {
            Name "OpaquePedestal"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            ZWrite On
            ZTest LEqual
            Cull Back
            Blend One Zero

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragBase

            half4 FragBase(Varyings IN) : SV_Target
            {
                if (IsSphereUV(IN.uv))
                    clip(-1);

                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                half3 lit = ApplySimpleLit(tex.rgb, IN.normalWS);
                return half4(lit, 1.0h);
            }
            ENDHLSL
        }

        // PASS 2: Transparent Sphere Dome (ZWrite Off so inner particles & words render clearly inside)
        Pass
        {
            Name "TransparentDome"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite Off
            ZTest LEqual
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragDome

            half4 FragDome(Varyings IN) : SV_Target
            {
                if (!IsSphereUV(IN.uv))
                    clip(-1);

                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(IN.viewDirWS);
                half fresnel = pow(1.0h - saturate(dot(N, V)), 2.2h);

                half3 glassCol = tex.rgb * 1.15h + half3(0.28h, 0.18h, 0.45h) * fresnel;
                half alpha = saturate(_GlassAlpha + fresnel * 0.22h);
                return half4(glassCol, alpha);
            }
            ENDHLSL
        }
    }
}

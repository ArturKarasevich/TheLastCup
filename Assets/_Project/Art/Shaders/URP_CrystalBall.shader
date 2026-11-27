Shader "TheLastCup/URP_CrystalBall"
{
    Properties
    {
        _BaseMap ("Base Map", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1, 1, 1, 1)
        _BumpMap ("Normal Map", 2D) = "bump" {}
        _BumpScale ("Normal Scale", Float) = 1.0
        _MaskMap ("Mask Map (R=Metallic, G=AO, A=Smoothness)", 2D) = "white" {}
        _EmissionMap ("Emission Map", 2D) = "black" {}
        [HDR] _EmissionColor ("Emission Color", Color) = (1, 1, 1, 1)
        _GlassAlpha ("Sphere Glass Alpha", Range(0.05, 0.8)) = 0.26
        _SwirlSpeed ("Sphere Swirl Speed", Float) = 0.3
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
        TEXTURE2D(_BumpMap);
        SAMPLER(sampler_BumpMap);
        TEXTURE2D(_MaskMap);
        SAMPLER(sampler_MaskMap);
        TEXTURE2D(_EmissionMap);
        SAMPLER(sampler_EmissionMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half _BumpScale;
            half4 _EmissionColor;
            half _GlassAlpha;
            half _SwirlSpeed;
        CBUFFER_END

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS   : NORMAL;
            float4 tangentOS  : TANGENT;
            float2 uv         : TEXCOORD0;
        };

        struct Varyings
        {
            float4 positionCS  : SV_POSITION;
            float2 uv          : TEXCOORD0;
            float3 normalWS    : TEXCOORD1;
            float4 tangentWS   : TEXCOORD2;
            float3 viewDirWS   : TEXCOORD3;
        };

        Varyings Vert(Attributes IN)
        {
            Varyings OUT;
            VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
            VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

            OUT.positionCS = posInputs.positionCS;
            OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
            OUT.normalWS = normInputs.normalWS;
            OUT.tangentWS = float4(normInputs.tangentWS, IN.tangentOS.w * GetOddNegativeScale());
            OUT.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);
            return OUT;
        }

        // In GreyBox_For_Textures_MagicSphere.001_BaseMap.png:
        // Wood pedestal is warm brown (R > B), sphere dome is purple/violet (B > R and B > G).
        bool IsSphereColor(half4 baseTex)
        {
            return (baseTex.b > baseTex.r && baseTex.b > baseTex.g);
        }

        float3 GetWorldNormal(Varyings IN)
        {
            float3 N = normalize(IN.normalWS);
            float3 T = normalize(IN.tangentWS.xyz);
            float3 B = cross(N, T) * IN.tangentWS.w;
            half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uv), _BumpScale);
            return normalize(TransformTangentToWorld(normalTS, half3x3(T, B, N)));
        }

        half3 ApplySimpleLit(half3 albedo, float3 normalWS, float3 viewDirWS, half ao, half smoothness)
        {
            Light mainLight = GetMainLight();
            float3 N = normalize(normalWS);
            float3 L = normalize(mainLight.direction);
            float3 V = normalize(viewDirWS);

            half NdotL = saturate(dot(N, L)) * 0.65h + 0.35h;
            half3 ambient = SampleSH(N) * ao;
            half3 lit = albedo * (mainLight.color * NdotL + ambient * 0.55h);

            float3 H = normalize(L + V);
            half specPower = lerp(8.0h, 64.0h, smoothness);
            half spec = pow(saturate(dot(N, H)), specPower) * smoothness * 0.35h;
            return lit + mainLight.color * spec;
        }
        ENDHLSL

        // PASS 1: Opaque Base / Pedestal
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
                half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                if (IsSphereColor(baseTex))
                    clip(-1);

                half4 mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, IN.uv);
                half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, IN.uv).rgb * _EmissionColor.rgb;
                float3 N = GetWorldNormal(IN);

                half3 albedo = baseTex.rgb * _BaseColor.rgb;
                half3 lit = ApplySimpleLit(albedo, N, IN.viewDirWS, mask.g, mask.a);
                return half4(lit + emission, 1.0h);
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
                half4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                if (!IsSphereColor(baseTex))
                    clip(-1);

                float2 uvCenter = float2(0.62, 0.23);
                float2 uvScale = float2(0.36, 0.22);
                float2 normUV = (IN.uv - uvCenter) / uvScale;
                float ang = _Time.y * _SwirlSpeed;
                float s = sin(ang);
                float c = cos(ang);
                float2 rotNorm = float2(normUV.x * c - normUV.y * s, normUV.x * s + normUV.y * c);
                float2 rotUV = uvCenter + rotNorm * uvScale;

                half4 rotBase = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, rotUV);
                if (!IsSphereColor(rotBase))
                {
                    rotUV = IN.uv;
                    rotBase = baseTex;
                }

                half4 mask = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, rotUV);
                half3 emission = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, rotUV).rgb * _EmissionColor.rgb;
                float3 N = GetWorldNormal(IN);
                float3 V = normalize(IN.viewDirWS);

                half fresnel = pow(1.0h - saturate(dot(N, V)), 2.2h);
                half3 glassCol = rotBase.rgb * _BaseColor.rgb * 1.15h
                               + half3(0.28h, 0.18h, 0.45h) * fresnel
                               + emission * 1.25h;
                half alpha = saturate(_GlassAlpha + fresnel * 0.22h + max(emission.r, emission.b) * 0.35h + mask.a * 0.05h);
                return half4(glassCol, alpha);
            }
            ENDHLSL
        }
    }
}

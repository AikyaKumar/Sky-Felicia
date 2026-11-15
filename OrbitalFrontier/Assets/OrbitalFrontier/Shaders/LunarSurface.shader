Shader "Orbital Frontier/Lunar Surface"
{
    Properties
    {
        _BaseMap ("Lunar albedo", 2D) = "white" {}
        _SunDir ("Light direction", Vector) = (0.5,0.1,0.8,0)
        _Tint ("Tint", Color) = (0.94,0.94,1,1)
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Geometry" "RenderType"="Opaque"}
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma target 3.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            float4 _SunDir;
            float4 _Tint;
            CBUFFER_END
            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };
            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };
            Varyings Vert(Attributes v) {
                Varyings o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.uv = v.uv;
                return o;
            }
            half4 Frag(Varyings i) : SV_Target {
                half3 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv).rgb;
                half sunlight = saturate(dot(normalize(i.normalWS), normalize(_SunDir.xyz)));
                half lighting = 0.025 + 1.34 * pow(sunlight, 0.82);
                return half4(albedo * lighting * _Tint.rgb, 1);
            }
            ENDHLSL
        }
    }
}

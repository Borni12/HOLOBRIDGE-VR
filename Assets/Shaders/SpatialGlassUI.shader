Shader "OmniOS/SpatialGlassUI"
{
    Properties
    {
        _BaseColor ("Base Tint", Color) = (0.08, 0.10, 0.14, 0.78)
        _BorderColor ("Border Rim Color", Color) = (0.0, 0.85, 1.0, 0.95)
        _RimPower ("Rim Exponent", Range(0.5, 6.0)) = 2.5
        _BorderThickness ("Border Width", Range(0.01, 0.2)) = 0.04
        _GlowIntensity ("Glow Intensity", Range(0.5, 4.0)) = 1.6
    }
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
        }
        LOD 100

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "GlassForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 viewDirWS  : TEXCOORD1;
                float2 uv         : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BorderColor;
                float _RimPower;
                float _BorderThickness;
                float _GlowIntensity;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = normalize(_WorldSpaceCameraPos - worldPos);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // Fresnel view-angle rim glow
                float3 N = normalize(input.normalWS);
                float3 V = normalize(input.viewDirWS);
                float NdotV = saturate(dot(N, V));
                float fresnel = pow(1.0 - NdotV, _RimPower);

                // Quad edge border mask
                float2 d = min(input.uv, 1.0 - input.uv);
                float edgeDist = min(d.x, d.y);
                float borderMask = 1.0 - smoothstep(0.0, _BorderThickness, edgeDist);

                // Combine glass base with glowing border and rim
                half4 finalColor = _BaseColor;
                finalColor.rgb += _BorderColor.rgb * (borderMask * _GlowIntensity + fresnel * 0.4);
                finalColor.a = saturate(_BaseColor.a + borderMask * 0.4 + fresnel * 0.2);

                return finalColor;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

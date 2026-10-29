Shader "OmniOS/MetaverseFloor"
{
    Properties
    {
        _BaseColor ("Base Floor Color", Color) = (0.04, 0.05, 0.08, 1.0)
        _GridColor ("Neon Grid Color", Color) = (0.0, 0.85, 1.0, 1.0)
        _GridSpacing ("Grid Spacing", Float) = 1.0
        _GridThickness ("Grid Line Thickness", Range(0.005, 0.1)) = 0.02
        _GlowIntensity ("Glow Intensity", Range(0.5, 4.0)) = 1.8
        _Roughness ("Surface Roughness", Range(0.01, 1.0)) = 0.15
    }
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
        }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 worldPos   : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float2 uv         : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _GridColor;
                float _GridSpacing;
                float _GridThickness;
                float _GlowIntensity;
                float _Roughness;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.worldPos = worldPos;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // World space grid coordinates
                float2 coord = input.worldPos.xz / _GridSpacing;
                float2 gridDist = abs(frac(coord - 0.5) - 0.5);
                float lineMask = 1.0 - smoothstep(0.0, _GridThickness, min(gridDist.x, gridDist.y));

                // Hexagonal circular falloff around center platform
                float distFromCenter = length(input.worldPos.xz);
                float ringPulse = sin(distFromCenter * 1.5 - _Time.y * 1.2) * 0.5 + 0.5;
                float ringMask = smoothstep(0.05, 0.0, abs(frac(distFromCenter / 2.0) - 0.5));

                // Base glossy reflective floor
                half3 col = _BaseColor.rgb;

                // Combine grid lines and radial rings
                float combinedGlow = saturate(lineMask + ringMask * 0.6) * _GlowIntensity;
                col += _GridColor.rgb * combinedGlow;

                // Subtle edge fade for infinite feel
                float edgeFade = 1.0 - smoothstep(12.0, 30.0, distFromCenter);
                col *= edgeFade;

                return half4(col, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}

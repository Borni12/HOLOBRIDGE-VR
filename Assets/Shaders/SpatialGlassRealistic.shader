Shader "OmniOS/SpatialGlassRealistic"
{
    Properties
    {
        _BaseTint ("Base Glass Color", Color) = (0.06, 0.08, 0.12, 0.82)
        _BorderGlowColor ("Rim Edge Color", Color) = (0.0, 0.88, 1.0, 0.95)
        _RimPower ("Rim Exponent", Range(1.0, 8.0)) = 3.2
        _BorderThickness ("Border Width", Range(0.01, 0.15)) = 0.035
        _SpecularGloss ("Specular Sharpness", Range(10.0, 200.0)) = 80.0
        _ChromaticShift ("Chromatic Edge Shift", Range(0.0, 0.05)) = 0.015
    }
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
        }
        LOD 200

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "RealisticGlassForward"
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
                float3 normalWS   : TEXCOORD0;
                float3 viewDirWS  : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float3 worldPos   : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseTint;
                float4 _BorderGlowColor;
                float _RimPower;
                float _BorderThickness;
                float _SpecularGloss;
                float _ChromaticShift;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = normalize(_WorldSpaceCameraPos - worldPos);
                output.uv = input.uv;
                output.worldPos = worldPos;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 N = normalize(input.normalWS);
                float3 V = normalize(input.viewDirWS);
                float NdotV = saturate(dot(N, V));

                // 1. Fresnel Edge Glow (Meta/Apple Vision style)
                float fresnel = pow(1.0 - NdotV, _RimPower);

                // 2. Rectangular Edge Border Mask
                float2 d = min(input.uv, 1.0 - input.uv);
                float edgeDist = min(d.x, d.y);
                float borderMask = 1.0 - smoothstep(0.0, _BorderThickness, edgeDist);

                // 3. Simulated Directional Specular Sheen (Soft directional reflection)
                Light mainLight = GetMainLight();
                float3 L = normalize(mainLight.direction);
                float3 H = normalize(L + V);
                float NdotH = saturate(dot(N, H));
                float specular = pow(NdotH, _SpecularGloss) * 0.65;

                // 4. Subtle Chromatic Edge Tint
                float rBorder = 1.0 - smoothstep(0.0, _BorderThickness + _ChromaticShift, edgeDist);
                float bBorder = 1.0 - smoothstep(0.0, _BorderThickness - _ChromaticShift, edgeDist);

                half4 col = _BaseTint;
                col.rgb += specular * mainLight.color;
                col.r += rBorder * 0.25;
                col.b += bBorder * 0.45;

                // Combine rim, glow, and border
                col.rgb += _BorderGlowColor.rgb * (borderMask * 1.8 + fresnel * 0.45);
                col.a = saturate(_BaseTint.a + borderMask * 0.5 + fresnel * 0.25);

                return col;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

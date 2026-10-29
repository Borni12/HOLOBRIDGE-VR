Shader "OmniOS/CameraViewfinder"
{
    Properties
    {
        _MainTex ("Camera Feed", 2D) = "white" {}
        _VignetteStrength ("Lens Vignette", Range(0.0, 1.0)) = 0.35
        _ShowGrid ("Show Rule of Thirds Grid (0 or 1)", Float) = 1.0
        _ShowReticle ("Show Center Reticle (0 or 1)", Float) = 1.0
        _IsRecording ("Is Recording (0 or 1)", Float) = 0.0
        _Exposure ("Exposure", Range(0.5, 2.5)) = 1.0
    }
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
        }
        LOD 100

        Cull Back
        ZWrite On

        Pass
        {
            Name "ViewfinderForward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _VignetteStrength;
                float _ShowGrid;
                float _ShowReticle;
                float _IsRecording;
                float _Exposure;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * _Exposure;

                // 1. Optical Lens Corner Vignette
                float2 centerCoord = uv - 0.5;
                float distSq = dot(centerCoord, centerCoord);
                float vignette = 1.0 - smoothstep(0.15, 0.55, distSq) * _VignetteStrength;
                col.rgb *= vignette;

                // 2. Rule-of-Thirds Compositional Grid Lines
                if (_ShowGrid > 0.5)
                {
                    float lineThresh = 0.0025;
                    float gx1 = abs(uv.x - 0.333);
                    float gx2 = abs(uv.x - 0.666);
                    float gy1 = abs(uv.y - 0.333);
                    float gy2 = abs(uv.y - 0.666);

                    if (gx1 < lineThresh || gx2 < lineThresh || gy1 < lineThresh || gy2 < lineThresh)
                    {
                        col.rgb = lerp(col.rgb, half3(1, 1, 1), 0.35);
                    }
                }

                // 3. Center Auto-Focus Brackets
                if (_ShowReticle > 0.5)
                {
                    float2 c = abs(uv - 0.5);
                    if ((c.x < 0.05 && c.y < 0.05 && (c.x > 0.045 || c.y > 0.045)) || 
                        (c.x < 0.015 && c.y < 0.002) || (c.y < 0.015 && c.x < 0.002))
                    {
                        col.rgb = half3(0.0, 1.0, 0.7); // Bright Neon Cyan/Green reticle
                    }
                }

                // 4. Recording Pulsing Red Dot (Upper Left)
                if (_IsRecording > 0.5)
                {
                    float2 recPos = uv - float2(0.08, 0.90);
                    float recDist = length(recPos);
                    if (recDist < 0.02)
                    {
                        float pulse = sin(_Time.y * 6.0) * 0.5 + 0.5;
                        col.rgb = lerp(half3(1.0, 0.1, 0.1), half3(0.4, 0.0, 0.0), pulse);
                    }
                }

                return col;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

Shader "OmniGenesis/OmniGaussianURP"
{
    Properties
    {
        _SubsurfaceColor ("Subsurface Scatter Tint", Color) = (0.95, 0.45, 0.35, 1.0)
        _GlowIntensity ("Depth Glow Intensity", Range(0, 5)) = 1.2
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent" 
            "RenderPipeline" = "UniversalPipeline" 
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 viewDirWS : TEXCOORD1;
            };

            StructuredBuffer<float3> _Positions;
            StructuredBuffer<float3> _Scales;
            StructuredBuffer<float4> _Rotations;
            StructuredBuffer<float4> _Colors;
            StructuredBuffer<int> _MaterialTypes;

            float4 _SubsurfaceColor;
            float _GlowIntensity;

            // Quaternion rotation helper
            float3 RotateByQuaternion(float3 v, float4 q)
            {
                float3 t = 2.0 * cross(q.xyz, v);
                return v + q.w * t + cross(q.xyz, t);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;

                uint id = input.instanceID;
                float3 centerWS = _Positions[id];
                float3 scale = _Scales[id];
                float4 rot = _Rotations[id];
                float4 baseColor = _Colors[id];

                // Billboard quad oriented to camera
                float3 quadPos = input.vertex.xyz * scale;
                float3 rotatedQuad = RotateByQuaternion(quadPos, rot);

                // View direction aligned
                float3 cameraRight = UNITY_MATRIX_V[0].xyz;
                float3 cameraUp = UNITY_MATRIX_V[1].xyz;
                float3 worldPos = centerWS + (cameraRight * input.vertex.x * scale.x) + (cameraUp * input.vertex.y * scale.y);

                output.positionCS = TransformWorldToHClip(worldPos);
                output.uv = input.uv;
                output.color = baseColor;
                output.viewDirWS = GetCameraPositionWS() - worldPos;

                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // 2D Gaussian Radial Falloff
                float2 coord = input.uv * 2.0 - 1.0;
                float r2 = dot(coord, coord);
                if (r2 > 1.0) discard;

                float gaussianAlpha = exp(-2.5 * r2) * input.color.a;

                // Spherical Harmonics & View-Dependent Shading approximation
                float3 viewDir = normalize(input.viewDirWS);
                float fresnel = pow(1.0 - saturate(dot(viewDir, float3(0, 1, 0))), 2.5);

                float3 finalColor = input.color.rgb + fresnel * _GlowIntensity * float3(0.2, 0.4, 0.8);

                // Subsurface scattering on skin materials
                finalColor = lerp(finalColor, _SubsurfaceColor.rgb * finalColor, 0.25);

                return float4(finalColor, gaussianAlpha);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

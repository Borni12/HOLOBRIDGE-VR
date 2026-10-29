Shader "OmniOS/VideoRealityDome"
{
    Properties
    {
        _MainTex ("Video Texture", 2D) = "black" {}
        _Immersion ("Immersion Alpha", Range(0.0, 1.0)) = 0.0
        _DepthRelief ("Depth Relief", Range(0.0, 2.0)) = 0.3
        _TintColor ("Tint Color", Color) = (1, 1, 1, 1)
        _Exposure ("Exposure", Range(0.5, 3.0)) = 1.0
    }
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent-100" 
            "RenderPipeline" = "UniversalPipeline" 
        }
        LOD 100

        Cull Front // Inverted sphere so the camera inside sees the interior
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ForwardLit"
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
                float2 uv         : TEXCOORD0;
                float3 worldPos   : TEXCOORD1;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _TintColor;
                float _Immersion;
                float _DepthRelief;
                float _Exposure;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;

                // Spherical equirectangular UV mapping from normal vector
                float3 n = normalize(input.positionOS.xyz);
                float u = atan2(n.z, n.x) / (2.0 * PI) + 0.5;
                float v = asin(clamp(n.y, -1.0, 1.0)) / PI + 0.5;
                output.uv = float2(u, v);

                // Sample texture luminance for procedural depth relief
                float4 vidSample = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, output.uv, 0);
                float luma = dot(vidSample.rgb, float3(0.299, 0.587, 0.114));

                // Extrude vertex outward based on depth relief and immersion
                float3 offsetPos = input.positionOS.xyz + (input.normalOS * (luma * _DepthRelief * _Immersion));
                output.positionCS = TransformObjectToHClip(offsetPos);
                output.worldPos = TransformObjectToWorld(offsetPos);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                col.rgb *= _TintColor.rgb * _Exposure;
                col.a = _Immersion;

                return col;
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}

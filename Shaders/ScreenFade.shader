Shader "DDOIT/ScreenFade"
{
    Properties
    {
        _Color ("Fade Color", Color) = (0, 0, 0, 1)
        _FadeAlpha ("Fade Alpha", Range(0, 1)) = 0
        _Desaturation ("Desaturation", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Overlay"
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        ZTest Always
        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ScreenFade"

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _FadeAlpha;
                half _Desaturation;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half combinedAlpha = max(_Desaturation, _FadeAlpha);
                if (combinedAlpha < 0.001)
                    return half4(0, 0, 0, 0);

                half3 finalColor = _Color.rgb;

                if (_Desaturation > 0.001)
                {
                    float2 screenUV = input.positionCS.xy / _ScreenParams.xy;
                    half3 sceneColor = SampleSceneColor(screenUV);

                    // Luminance 기반 흑백 변환
                    half gray = dot(sceneColor, half3(0.299, 0.587, 0.114));
                    half3 desaturated = lerp(sceneColor, half3(gray, gray, gray), _Desaturation);

                    // 탈채도 + 페이드 색상 블렌드
                    finalColor = lerp(desaturated, _Color.rgb, _FadeAlpha);
                }

                return half4(finalColor, combinedAlpha);
            }
            ENDHLSL
        }
    }
}

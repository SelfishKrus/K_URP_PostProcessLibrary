Shader "Blit/Outline"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZWrite Off 
        Cull Off
        Pass
        {
            Name "DepthOutlinePass"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"

            // The Blit.hlsl file provides the vertex shader (Vert),
            // input structure (Attributes) and output strucutre (Varyings)
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment frag
            #pragma multi_compile _ _DEPTH_OUTLINE
            #pragma multi_compile _ _NORMAL_OUTLINE

            TEXTURE2D_X(_CamColTex);        SAMPLER(sampler_CamColTex);

            float _Intensity;
            float _Threshold;
            float _Thickness;
            float _Smoothness;
            float3 _OutlineColor;

            half4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float4 camColTex = SAMPLE_TEXTURE2D_X(_CamColTex, sampler_CamColTex, input.texcoord);
                float3 camCol = camColTex.rgb;
                float3 outCol;

                // sobel operator 
                const half Gx[9] = {
                    -1, 0, 1,
                    -2, 0, 2,
                    -1, 0, 1
                };
                const half Gy[9] = {
                    1, 2, 1,
                    0, 0, 0,
                    -1, -2, -1
                };

                #ifdef _DEPTH_OUTLINE
                    // Depth Outline // 
                    float depth = SampleSceneDepth(input.texcoord.xy).r;
                    depth = Linear01Depth(depth, _ZBufferParams);

                    // depth comparison
                    float edgeX_depth = 0;
                    float edgeY_depth = 0;

                    for (int i = 0; i < 9; i++) 
                    {
                        half2 offset = half2(i % 3, i / 3) - half2(1, 1);
                        half depthOffset = SampleSceneDepth(input.texcoord.xy + offset * _Thickness).r;
                        depthOffset = Linear01Depth(depthOffset, _ZBufferParams);
                        edgeX_depth += depthOffset * Gx[i];
                        edgeY_depth += depthOffset * Gy[i];
                    };

                    edgeX_depth /= 9;
                    edgeY_depth /= 9;

                    float edge_depth = (1 - sqrt(edgeX_depth * edgeX_depth + edgeY_depth * edgeY_depth));
                    edge_depth = smoothstep(_Threshold-_Smoothness, _Threshold+_Smoothness, edge_depth);

                    outCol = lerp(_OutlineColor, camCol, edge_depth);
                #endif

                #ifdef _NORMAL_OUTLINE
                    // Normal Outline // 
                    float3 normal = SampleSceneNormals(input.texcoord.xy).rgb;
                    
                    // normal comparison
                    float edgeX_normal = 0;
                    float edgeY_normal = 0;
                    
                    for (uint j = 0; j < 9; j++) 
                    {
                        half2 offset = half2(j % 3, j / 3) - half2(1, 1);
                        half3 normalOffset = SampleSceneNormals(input.texcoord.xy + offset * _Thickness).rgb;
                        edgeX_normal += normalOffset * Gx[j];
                        edgeY_normal += normalOffset * Gy[j];
                    };

                    edgeX_normal /= 9;
                    edgeY_normal /= 9;

                    float edge_normal = (1 - sqrt(edgeX_normal * edgeX_normal + edgeY_normal * edgeY_normal));
                    edge_normal = smoothstep(_Threshold-_Smoothness, _Threshold+_Smoothness, edge_normal);

                    outCol = edge_normal;
                #endif
                
                return float4(outCol, camColTex.a);
            }
            ENDHLSL
        }
    }
}
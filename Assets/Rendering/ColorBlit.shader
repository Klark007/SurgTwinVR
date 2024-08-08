// https://docs.unity3d.com/2020.2/Documentation/Manual/SinglePassInstancing.html

Shader "ColorBlit"
{
        SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZWrite Off Cull Off
        Pass
        {
            Name "ColorBlitPass"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // The Blit.hlsl file provides the vertex shader (Vert),
            // input structure (Attributes) and output strucutre (Varyings)
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            #pragma vertex Vert
            #pragma fragment frag

            TEXTURE2D_X(_CameraOpaqueTexture);
            SAMPLER(sampler_CameraOpaqueTexture);

            TEXTURE2D_X(_CameraDepthTexture); // try to access global from compute shader
            SAMPLER(sampler_CameraDepthTexture);
            
            uniform int custom_rt_res_x;
            uniform int custom_rt_res_y;

            uniform StructuredBuffer<uint> custom_rt_left;
            uniform StructuredBuffer<uint> custom_rt_right;

            float4 color_from_point(uint i_color);
            float4 get_rt_color(StructuredBuffer<uint> custom_rt, float2 uv, out float depth);

            uniform float left_near_clip_plane;
            uniform float left_far_clip_plane;
            uniform float right_near_clip_plane;
            uniform float right_far_clip_plane;

            static float depth_bias = -0.007; // before 0.05

            float linearize_depth(float d, float near, float far)
            {
                float normalized_dist = 2.0 * near * far / (far + near - d * (far - near));
                return (normalized_dist - near) / (far - near);
            }

            half4 frag (Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                    
                float4 color = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, input.texcoord);
                float sample_depth = 1 - SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, input.texcoord).r;
                sample_depth = (sample_depth * 2) - 1;
                float  depth;
                float4 pcd_color;
                float pcd_depth;

                if (unity_StereoEyeIndex == 0) {
                    depth = linearize_depth(sample_depth, left_near_clip_plane, left_far_clip_plane);
                    pcd_color = get_rt_color(custom_rt_left, input.texcoord, pcd_depth);
                } else if (unity_StereoEyeIndex == 1) {
                    depth = linearize_depth(sample_depth, right_near_clip_plane, right_far_clip_plane);
                    pcd_color = get_rt_color(custom_rt_right, input.texcoord, pcd_depth);
                } else {
                    return float4(1,0,1,1);
                }

                // DEBUGGING
                
                if (pcd_depth + depth_bias < 0 || pcd_depth + depth_bias > 1) {
                    pcd_color = float4(1,0,1,1);
                }
                
                //return (all(pcd_color == float4(0,0,0,1)))? float4(depth,depth,depth,1) : float4(pcd_depth,pcd_depth,pcd_depth,1);
                return (!all(pcd_color == float4(0,0,0,1)) && pcd_depth < depth) ? pcd_color : color;
            }

            float4 get_rt_color(StructuredBuffer<uint> custom_rt, float2 uv, out float depth) 
            {
                int screen_x = int(uv.x * custom_rt_res_x);
                int screen_y = int(uv.y * custom_rt_res_y);
                
                int idx = screen_y * custom_rt_res_x + screen_x;
                uint value = custom_rt[idx];
                depth = ((float((value >> 24) & 0xFF)) / 255) - depth_bias; // bias to prefer points

                return color_from_point(value);
            }

            float4 color_from_point(uint i_color)
            {
                float r = (float) (i_color & 0xFF);
                float g = (float) ((i_color >> 8) & 0xFF);
                float b = (float) ((i_color >> 16) & 0xFF);
    
                return float4(r / 255, g / 255, b / 255, 1);
            }

            ENDHLSL
        }
    }
}
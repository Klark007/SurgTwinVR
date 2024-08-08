using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

using System;
using System.IO;
using System.Text;


internal class ColorBlitRendererFeature : ScriptableRendererFeature
{
    static Encoding encA = Encoding.ASCII;

    public Shader m_Shader;

    public ComputeShader compute_shader_render;

    public string camera_name_left = "LeftEyeAnchor";
    public string camera_name_right = "RightEyeAnchor";

#nullable enable
    Camera? c_l = null;
    Camera? c_r = null;
#nullable disable

    Camera camera_left;
    Camera camera_right;

    Material m_Material;

    ColorBlitPass m_RenderPass = null;

    public override void AddRenderPasses(ScriptableRenderer renderer,
                                    ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
#nullable enable
            if (!((c_l is Camera cam_l) && (c_r is Camera cam_r)))
            {
               Debug.LogError("Cameras not found");
            }
#nullable disable

            renderer.EnqueuePass(m_RenderPass);
        }
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer,
                                        in RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
            // Calling ConfigureInput with the ScriptableRenderPassInput.Color argument
            // ensures that the opaque texture is available to the Render Pass.
            m_RenderPass.ConfigureInput(ScriptableRenderPassInput.Color);
            m_RenderPass.SetTarget(renderer.cameraColorTargetHandle, compute_shader_render, camera_left, camera_right);
        }
    }


    public override void Create()
    {
        // find left and right camera
#nullable enable
        foreach (Camera c in FindObjectsOfType<Camera>())
        {
            if (c.name == camera_name_left)
            {
                c_l = c;
            }
            if (c.name == camera_name_right)
            {
                c_r = c;
            }
        }

        if ((c_l is Camera cam_l) && (c_r is Camera cam_r))
        {
            camera_left = cam_l;
            camera_right = cam_r;
        }
        else
        {
            Debug.LogError("Cameras not found");
        }
#nullable disable

        // unity stuff
        m_Material = CoreUtils.CreateEngineMaterial(m_Shader);
        m_RenderPass = new ColorBlitPass(m_Material);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(m_Material);
    }
}
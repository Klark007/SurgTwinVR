using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Profiling;


// custom pass which renders point cloud and copies it to camera texture
internal class ColorBlitPass : ScriptableRenderPass
{
    ProfilingSampler m_ProfilingSampler = new ProfilingSampler("ColorBlit");
    Material m_Material;
    RTHandle m_CameraColorTarget;

    ComputeShader cs_render;

    AnimationLoader loader;

    Camera camera_left;
    Camera center_camera;
    Camera camera_right;

    public ColorBlitPass(Material material)
    {
        m_Material = material;
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public void SetTarget(RTHandle colorHandle, ComputeShader shader_render, Camera cam_left, Camera cam_right)
    {
        m_CameraColorTarget = colorHandle;

        cs_render = shader_render;

        camera_left = cam_left;
        camera_right = cam_right;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        ConfigureTarget(m_CameraColorTarget);
    }

    // writes point clouds from compute buffer provided by the AnimationLoader to a custom texture
    private void compute_textures()
    {
        int render_cs_idx = cs_render.FindKernel("CSMain");
        int clear_cs_idx = cs_render.FindKernel("CSClear");

        // model, view, projection matrices
        // ground truth: center_camera.WorldToViewportPoint(center, Camera.MonoOrStereoscopicEye.Left);
        Matrix4x4 model_mat = Matrix4x4.Scale(new Vector3(-0.001F,0.001F,0.001F)) * Matrix4x4.Translate(new Vector3(0,0,0)) * Matrix4x4.Rotate(Quaternion.Euler(-90, 0, 0));
        
        Matrix4x4 view_mat_left = camera_left.worldToCameraMatrix;
        Matrix4x4 proj_mat_left = center_camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left); 
        Matrix4x4 mvp_mat_left = proj_mat_left * view_mat_left * model_mat;
        cs_render.SetMatrix("mvp_mat_left", mvp_mat_left);

        Matrix4x4 view_mat_right = camera_right.worldToCameraMatrix;
        Matrix4x4 proj_mat_right = center_camera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
        Matrix4x4 mvp_mat_right = proj_mat_right * view_mat_right * model_mat;
        cs_render.SetMatrix("mvp_mat_right", mvp_mat_right);


        // clear custom render targets
        cs_render.SetInt("custom_rt_res_x", loader.res_x);
        cs_render.SetInt("custom_rt_res_y", loader.res_y);
        cs_render.SetBuffer(clear_cs_idx, "custom_rt_left", loader.custom_rt_left);
        cs_render.SetBuffer(clear_cs_idx, "custom_rt_right", loader.custom_rt_right);

        cs_render.Dispatch(clear_cs_idx, loader.res_x / 8, loader.res_y / 8, 1);


        // write points to custom render target
        cs_render.SetBuffer(render_cs_idx, "custom_rt_left", loader.custom_rt_left);
        cs_render.SetBuffer(render_cs_idx, "custom_rt_right", loader.custom_rt_right);

        cs_render.SetBuffer(render_cs_idx, "pointBuffer", loader.pointBuffer);

        cs_render.SetFloat("left_near_clip_plane", camera_left.nearClipPlane);
        cs_render.SetFloat("left_far_clip_plane", camera_left.farClipPlane);
        cs_render.SetFloat("right_near_clip_plane", camera_right.nearClipPlane);
        cs_render.SetFloat("right_far_clip_plane", camera_right.farClipPlane);

        cs_render.Dispatch(render_cs_idx, (int) System.Math.Ceiling((double) loader.nrPoints / 16), 1, 1);


        // setup blit
        m_Material.SetInt("custom_rt_res_x", loader.res_x);
        m_Material.SetInt("custom_rt_res_y", loader.res_y);
        m_Material.SetBuffer("custom_rt_left", loader.custom_rt_left);
        m_Material.SetBuffer("custom_rt_right", loader.custom_rt_right);

        m_Material.SetFloat("left_near_clip_plane", camera_left.nearClipPlane);
        m_Material.SetFloat("left_far_clip_plane", camera_left.farClipPlane);
        m_Material.SetFloat("right_near_clip_plane", camera_right.nearClipPlane);
        m_Material.SetFloat("right_far_clip_plane", camera_right.farClipPlane);
    }

    private void clearOnly()
    {
        int clear_cs_idx = cs_render.FindKernel("CSClear");

        // clear custom render targets
        cs_render.SetInt("custom_rt_res_x", loader.res_x);
        cs_render.SetInt("custom_rt_res_y", loader.res_y);
        cs_render.SetBuffer(clear_cs_idx, "custom_rt_left", loader.custom_rt_left);
        cs_render.SetBuffer(clear_cs_idx, "custom_rt_right", loader.custom_rt_right);

        cs_render.Dispatch(clear_cs_idx, loader.res_x / 8, loader.res_y / 8, 1);

        // setup blit
        m_Material.SetInt("custom_rt_res_x", loader.res_x);
        m_Material.SetInt("custom_rt_res_y", loader.res_y);
        m_Material.SetBuffer("custom_rt_left", loader.custom_rt_left);
        m_Material.SetBuffer("custom_rt_right", loader.custom_rt_right);

        m_Material.SetFloat("left_near_clip_plane", camera_left.nearClipPlane);
        m_Material.SetFloat("left_far_clip_plane", camera_left.farClipPlane);
        m_Material.SetFloat("right_near_clip_plane", camera_right.nearClipPlane);
        m_Material.SetFloat("right_far_clip_plane", camera_right.farClipPlane);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
       var cameraData = renderingData.cameraData;
        if (cameraData.camera.cameraType != CameraType.Game)
            return;

        if (m_Material == null)
            return;


        if (cameraData.camera.name != "CenterEyeAnchor")
        {
            Debug.LogError(cameraData.camera.name);
            return;
        }

        center_camera = cameraData.camera;
        loader = center_camera.GetComponent<AnimationLoader>();
        Profiler.BeginSample("Render texture");

        if (!center_camera.GetComponent<AnimationLoader>().animationActive)
        {
            clearOnly();
        } else
        {
            compute_textures();
        }
        
        Profiler.EndSample();

        Profiler.BeginSample("Blit texture");
        CommandBuffer cmd = CommandBufferPool.Get();
        using (new ProfilingScope(cmd, m_ProfilingSampler))
        {
            Blitter.BlitCameraTexture(cmd, m_CameraColorTarget, m_CameraColorTarget, m_Material, 0);
        }
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();

        CommandBufferPool.Release(cmd);
        Profiler.EndSample();
    }

}
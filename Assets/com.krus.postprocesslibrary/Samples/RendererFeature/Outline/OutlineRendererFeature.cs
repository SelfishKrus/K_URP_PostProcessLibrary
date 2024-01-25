using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal class OutlineRendererFeature : ScriptableRendererFeature
{   

    //////////////
    // Settings // 
    //////////////

    [System.Serializable]
    public class OutlineSettings 
    {   
        [Header("Render Pass")]
        public Shader m_shader;
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public string colorTargetDestinationID = "_CamColTex";

        [Header("Shader Params")]
        [Range(0.0f, 1.0f)]
        public float threshold = 0.5f;
        [Range(0.0f, 0.03f)]
        public float thickness = 0.01f;
        [Range(0.0f, 1.0f)]
        public float smoothness = 0.1f;

        public Color outlineColor = Color.black;
        
    }

    //////////////////////
    // Renderer Feature // 
    //////////////////////

    public OutlineSettings settings = new OutlineSettings();

    Material m_Material;

    DepthOutlineRenderPass m_RenderPass = null;

    

    public override void Create()
    {
        m_Material = CoreUtils.CreateEngineMaterial(settings.m_shader);
        m_RenderPass = new DepthOutlineRenderPass(m_Material, settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer,
                                    ref RenderingData renderingData)
    {
        // if (renderingData.cameraData.camera.cameraType != CameraType.Game && renderingData.cameraData.camera.cameraType != CameraType.SceneView)
            renderer.EnqueuePass(m_RenderPass);
            m_RenderPass.ConfigureInput(ScriptableRenderPassInput.Normal);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer,
                                        in RenderingData renderingData)
    {
        // if (renderingData.cameraData.camera.cameraType != CameraType.Game && renderingData.cameraData.camera.cameraType != CameraType.SceneView)
        // {
            // Calling ConfigureInput with the ScriptableRenderPassInput.Color argument
            // ensures that the opaque texture is available to the Render Pass.
            m_RenderPass.ConfigureInput(ScriptableRenderPassInput.Color);
            m_RenderPass.SetTarget(renderer.cameraColorTargetHandle);
        // }
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(m_Material);
    }

    //////////////////////
    //   Renderer Pass  // 
    //////////////////////

    internal class DepthOutlineRenderPass : ScriptableRenderPass
    {   
        ProfilingSampler m_profilingSampler = new ProfilingSampler("DepthOutline");
        Material m_material;
        RTHandle m_cameraColorTarget;
        RTHandle rtCustomColor, rtTempColor;
        OutlineRendererFeature.OutlineSettings m_settings;

        public DepthOutlineRenderPass(Material material, OutlineRendererFeature.OutlineSettings settings)
        {   
            m_material = material;
            this.m_settings = settings;
            renderPassEvent = m_settings.renderPassEvent;
        }

        public void SetTarget(RTHandle colorHandle)
        {
            m_cameraColorTarget = colorHandle;
        }

        // Pass Data //

        public void PassShaderData(Material material)
        {
           material.SetFloat("_Threshold", m_settings.threshold);
           material.SetFloat("_Thickness", m_settings.thickness);
           material.SetFloat("_Smoothness", m_settings.smoothness);
           material.SetVector("_OutlineColor", m_settings.outlineColor);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {   
            var colorDesc = renderingData.cameraData.cameraTargetDescriptor;
            colorDesc.depthBufferBits = 0;

            // Set up temporary color buffer (for blit)
            RenderingUtils.ReAllocateIfNeeded(ref rtCustomColor, colorDesc, name: "_RTCustomColor");
            RenderingUtils.ReAllocateIfNeeded(ref rtTempColor, colorDesc, name: "_RTTempColor");

            ConfigureTarget(m_cameraColorTarget);
            ConfigureTarget(rtCustomColor);
            ConfigureTarget(rtTempColor);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cameraData = renderingData.cameraData;

            if (m_material == null)
                return;

            CommandBuffer cmd = CommandBufferPool.Get();
            using (UnityEngine.Rendering.ProfilingScope profilingScope = new UnityEngine.Rendering.ProfilingScope(cmd, m_profilingSampler))
            {
                PassShaderData(m_material);
                m_material.SetTexture(m_settings.colorTargetDestinationID, m_cameraColorTarget);

                RTHandle rtCamera = renderingData.cameraData.renderer.cameraColorTargetHandle;

                Blitter.BlitCameraTexture(cmd, m_cameraColorTarget, rtCustomColor, m_material, 0);
                Blitter.BlitCameraTexture(cmd, rtCustomColor, m_cameraColorTarget);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }
    }
}
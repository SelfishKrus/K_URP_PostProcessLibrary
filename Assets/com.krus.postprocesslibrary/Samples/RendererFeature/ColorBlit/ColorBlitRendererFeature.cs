using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal class ColorBlitRendererFeature : ScriptableRendererFeature
{   

    //////////////
    // Settings // 
    //////////////

    [System.Serializable]
    public class Settings 
    {   
        [Header("Render Pass")]
        public Shader m_shader;
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
        public string colorTargetDestinationID = "_CamColTex";

        [Header("Shader Params")]
        public float intensity = 1.0f;
    }

    

    //////////////////////
    // Renderer Feature // 
    //////////////////////

    public Settings settings = new Settings();

    Material m_Material;

    ColorBlitRenderPass m_RenderPass = null;

    public override void Create()
    {
        m_Material = CoreUtils.CreateEngineMaterial(settings.m_shader);
        m_RenderPass = new ColorBlitRenderPass(m_Material, settings);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer,
                                    ref RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
            renderer.EnqueuePass(m_RenderPass);
    }

    public override void SetupRenderPasses(ScriptableRenderer renderer,
                                        in RenderingData renderingData)
    {
        if (renderingData.cameraData.cameraType == CameraType.Game)
        {
            // Calling ConfigureInput with the ScriptableRenderPassInput.Color argument
            // ensures that the opaque texture is available to the Render Pass.
            m_RenderPass.ConfigureInput(ScriptableRenderPassInput.Color);
            m_RenderPass.SetTarget(renderer.cameraColorTargetHandle);
        }
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(m_Material);
    }
}
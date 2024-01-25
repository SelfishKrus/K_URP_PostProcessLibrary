using System.ComponentModel;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

internal class ColorBlitRenderPass : ScriptableRenderPass
{   
    ProfilingSampler m_profilingSampler = new ProfilingSampler("ColorBlit");
    Material m_material;
    RTHandle m_cameraColorTarget;
    RTHandle rtCustomColor, rtTempColor;
    ColorBlitRendererFeature.ColorBlitSettings m_settings;

    public ColorBlitRenderPass(Material material, ColorBlitRendererFeature.ColorBlitSettings settings)
    {   
        m_material = material;
        this.m_settings = settings;
        renderPassEvent = m_settings.renderPassEvent;
    }

    public void SetTarget(RTHandle colorHandle)
    {
        m_cameraColorTarget = colorHandle;
    }

    public void PassShaderData(Material material)
    {
        material.SetFloat("_Intensity", m_settings.intensity);
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
        using (new ProfilingScope(cmd, m_profilingSampler))
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
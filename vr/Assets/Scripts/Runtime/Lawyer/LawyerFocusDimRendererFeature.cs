using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace NCKH.VR.Lawyer
{
    public sealed class LawyerFocusDimRendererFeature : ScriptableRendererFeature
    {
        [SerializeField] private Material dimMaterial;
        [SerializeField] private LayerMask boardLayerMask;
        [SerializeField] private uint foregroundRenderingLayerMask = 1u << 31;
        private LawyerFocusDimPass dimPass;
        private LawyerBoardOverlayPass overlayPass;

#if UNITY_EDITOR
        public void ConfigureInEditor(
            Material material, LayerMask configuredBoardLayerMask,
            uint configuredForegroundRenderingLayerMask)
        {
            dimMaterial = material;
            boardLayerMask = configuredBoardLayerMask;
            foregroundRenderingLayerMask = configuredForegroundRenderingLayerMask;
            Create();
        }
#endif

        public Material DimMaterial => dimMaterial;
        public LayerMask BoardLayerMask => boardLayerMask;
        public uint ForegroundRenderingLayerMask => foregroundRenderingLayerMask;

        public override void Create()
        {
            dimPass = new LawyerFocusDimPass(dimMaterial)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents
            };
            overlayPass = new LawyerBoardOverlayPass(
                dimMaterial, boardLayerMask, foregroundRenderingLayerMask)
            {
                renderPassEvent = RenderPassEvent.AfterRenderingTransparents
            };
        }

        public override void AddRenderPasses(
            ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (dimMaterial == null || renderingData.cameraData.cameraType != CameraType.Game)
                return;
            dimPass.SetMaterial(dimMaterial);
            renderer.EnqueuePass(dimPass);
            bool resetDepth = LawyerBoardFocusLighting.CurrentOverlayActive;
            if ((LawyerBoardFocusLighting.CurrentScreenDimAmount > 0f || resetDepth)
                && boardLayerMask.value != 0 && foregroundRenderingLayerMask != 0)
            {
                overlayPass.Configure(
                    dimMaterial, boardLayerMask, foregroundRenderingLayerMask,
                    resetDepth);
                renderer.EnqueuePass(overlayPass);
            }
        }

        protected override void Dispose(bool disposing)
        {
            overlayPass = null;
        }

        private sealed class LawyerFocusDimPass : ScriptableRenderPass
        {
            private const string PassName = "Lawyer Focus Dim";
            private Material material;

            private sealed class PassData
            {
                public Material Material;
            }

            public LawyerFocusDimPass(Material configuredMaterial)
            {
                material = configuredMaterial;
            }

            public void SetMaterial(Material configuredMaterial) => material = configuredMaterial;

            public override void RecordRenderGraph(
                RenderGraph renderGraph, ContextContainer frameData)
            {
                if (material == null)
                    return;
                UniversalResourceData resources = frameData.Get<UniversalResourceData>();
                using (IRasterRenderGraphBuilder builder =
                    renderGraph.AddRasterRenderPass<PassData>(PassName, out PassData passData))
                {
                    passData.Material = material;
                    builder.SetRenderAttachment(
                        resources.activeColorTexture, 0, AccessFlags.Write);
                    builder.SetRenderAttachmentDepth(
                        resources.activeDepthTexture, AccessFlags.Read);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                        context.cmd.DrawProcedural(
                            Matrix4x4.identity, data.Material, 0,
                            MeshTopology.Triangles, 3, 1));
                }
            }

        }

        private sealed class LawyerBoardOverlayPass : ScriptableRenderPass
        {
            private const string PassName = "Lawyer Board And Foreground Restore";
            private const int ResetDepthMaterialPass = 1;
            private static readonly List<ShaderTagId> ShaderTags = new List<ShaderTagId>
            {
                new ShaderTagId("UniversalForwardOnly"),
                new ShaderTagId("UniversalForward"),
                new ShaderTagId("SRPDefaultUnlit"),
                new ShaderTagId("LightweightForward")
            };

            private Material material;
            private LayerMask configuredBoardLayerMask;
            private uint configuredForegroundRenderingLayerMask;
            private bool resetDepth;

            private sealed class PassData
            {
                public Material Material;
                public bool ResetDepth;
                public RendererListHandle BoardOpaque;
                public RendererListHandle BoardTransparent;
                public RendererListHandle ForegroundOpaque;
                public RendererListHandle ForegroundTransparent;
            }

            public LawyerBoardOverlayPass(
                Material configuredMaterial, LayerMask boardMask, uint foregroundMask)
            {
                material = configuredMaterial;
                configuredBoardLayerMask = boardMask;
                configuredForegroundRenderingLayerMask = foregroundMask;
            }

            public void Configure(
                Material configuredMaterial, LayerMask boardMask, uint foregroundMask,
                bool shouldResetDepth)
            {
                material = configuredMaterial;
                configuredBoardLayerMask = boardMask;
                configuredForegroundRenderingLayerMask = foregroundMask;
                resetDepth = shouldResetDepth;
            }

            public override void RecordRenderGraph(
                RenderGraph renderGraph, ContextContainer frameData)
            {
                if (material == null
                    || resetDepth && material.passCount <= ResetDepthMaterialPass)
                    return;

                UniversalResourceData resources = frameData.Get<UniversalResourceData>();
                UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalLightData lightData = frameData.Get<UniversalLightData>();

                using (IRasterRenderGraphBuilder builder =
                    renderGraph.AddRasterRenderPass<PassData>(PassName, out PassData passData))
                {
                    passData.Material = material;
                    passData.ResetDepth = resetDepth;
                    passData.BoardOpaque = CreateRendererList(
                        renderGraph, renderingData, cameraData, lightData,
                        RenderQueueRange.opaque, configuredBoardLayerMask,
                        uint.MaxValue, cameraData.defaultOpaqueSortFlags);
                    passData.BoardTransparent = CreateRendererList(
                        renderGraph, renderingData, cameraData, lightData,
                        RenderQueueRange.transparent, configuredBoardLayerMask,
                        uint.MaxValue, SortingCriteria.CommonTransparent);
                    passData.ForegroundOpaque = CreateRendererList(
                        renderGraph, renderingData, cameraData, lightData,
                        RenderQueueRange.opaque, -1,
                        configuredForegroundRenderingLayerMask,
                        cameraData.defaultOpaqueSortFlags);
                    passData.ForegroundTransparent = CreateRendererList(
                        renderGraph, renderingData, cameraData, lightData,
                        RenderQueueRange.transparent, -1,
                        configuredForegroundRenderingLayerMask,
                        SortingCriteria.CommonTransparent);

                    builder.UseRendererList(passData.BoardOpaque);
                    builder.UseRendererList(passData.BoardTransparent);
                    builder.UseRendererList(passData.ForegroundOpaque);
                    builder.UseRendererList(passData.ForegroundTransparent);
                    builder.SetRenderAttachment(
                        resources.activeColorTexture, 0, AccessFlags.Write);
                    builder.SetRenderAttachmentDepth(
                        resources.activeDepthTexture, AccessFlags.ReadWrite);
                    builder.AllowPassCulling(false);
                    builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                    {
                        if (data.ResetDepth)
                        {
                            context.cmd.DrawProcedural(
                                Matrix4x4.identity, data.Material, ResetDepthMaterialPass,
                                MeshTopology.Triangles, 3, 1);
                        }
                        context.cmd.DrawRendererList(data.BoardOpaque);
                        context.cmd.DrawRendererList(data.BoardTransparent);
                        context.cmd.DrawRendererList(data.ForegroundOpaque);
                        context.cmd.DrawRendererList(data.ForegroundTransparent);
                    });
                }
            }

            private RendererListHandle CreateRendererList(
                RenderGraph renderGraph,
                UniversalRenderingData renderingData,
                UniversalCameraData cameraData,
                UniversalLightData lightData,
                RenderQueueRange renderQueueRange,
                LayerMask layerMask,
                uint renderingLayerMask,
                SortingCriteria sortingCriteria)
            {
                FilteringSettings filteringSettings =
                    new FilteringSettings(renderQueueRange, layerMask)
                    {
                        renderingLayerMask = renderingLayerMask
                    };
                DrawingSettings drawingSettings = RenderingUtils.CreateDrawingSettings(
                    ShaderTags, renderingData, cameraData, lightData, sortingCriteria);
                RendererListParams rendererListParams = new RendererListParams(
                    renderingData.cullResults, drawingSettings, filteringSettings);
                return renderGraph.CreateRendererList(rendererListParams);
            }
        }
    }
}

using System.Diagnostics;
using System.Runtime.InteropServices;
using Vortice.Direct3D;
using Vortice.Direct3D12;

namespace Direct3D12
{
    static class D3D12PostProcess
    {
        private const int RP_Count = 1;
        private const int RP_RootConstants = 0;

        [StructLayout(LayoutKind.Sequential)]
        struct PipelineStateStream
        {
            public PipelineStateSubObjectTypeRootSignature RootSignature;
            public PipelineStateSubObjectTypeVertexShader Vs;
            public PipelineStateSubObjectTypePixelShader Ps;
            public PipelineStateSubObjectTypePrimitiveTopology PrimitiveTopology;
            public PipelineStateSubObjectTypeRenderTargetFormats RenderTargetFormats;
            public PipelineStateSubObjectTypeRasterizer Rasterizer;
        }

        private static ID3D12RootSignature fxRootSig = null;
        private static ID3D12PipelineState fxPso = null;

        public static bool Initialize()
        {
            return CreateFxPsoAndRootSignature();
        }
        private static bool CreateFxPsoAndRootSignature()
        {
            Debug.Assert(fxRootSig == null && fxPso == null);

            // Create FX root signature
            var parameters = new RootParameter1[RP_Count];
            parameters[RP_RootConstants] = D3D12Helpers.AsConstants(1, ShaderVisibility.Pixel, 1);

            var rootSignature = new D3D12RootSignatureDesc(parameters);
            rootSignature.Flags &= ~RootSignatureFlags.DenyPixelShaderRootAccess;
            fxRootSig = rootSignature.Create();
            Debug.Assert(fxRootSig != null);
            D3D12Helpers.NameD3D12Object(fxRootSig, "Post-process FX Root Signature");

            // Create FX PSO
            PipelineStateStream pipelineState = new()
            {
                RootSignature = new PipelineStateSubObjectTypeRootSignature(fxRootSig),
                Vs = new(D3D12Shaders.GetEngineShader(EngineShaders.FullScreenTriangleVs).Span),
                Ps = new(D3D12Shaders.GetEngineShader(EngineShaders.PostProcessPs).Span),
                PrimitiveTopology = new(PrimitiveTopologyType.Triangle),
                RenderTargetFormats = new([D3D12Surface.DefaultBackBufferFormat]),
                Rasterizer = new(D3D12Helpers.RasterizerStatesCollection.NoCull),
            };

            fxPso = D3D12Graphics.Device.CreatePipelineState(pipelineState);
            D3D12Helpers.NameD3D12Object(fxPso, "Post-process FX Pipeline State Object");

            return fxRootSig != null && fxPso != null;
        }

        public static void Shutdown()
        {
            fxRootSig?.Dispose();
            fxRootSig = null;
            fxPso?.Dispose();
            fxPso = null;
        }

        public static void PostProcess(ID3D12GraphicsCommandList cmdList, CpuDescriptorHandle targetRtv)
        {
            cmdList.SetGraphicsRootSignature(fxRootSig);
            cmdList.SetPipelineState(fxPso);

            cmdList.SetGraphicsRoot32BitConstant(RP_RootConstants, D3D12GPass.MainBuffer.Srv.Index, 0);
            cmdList.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            // NOTE: we don't need to clear the render target, because each pixel will 
            //       be overwritten by pixels from gpass main buffer.
            //       We also don't need a depth buffer.
            cmdList.OMSetRenderTargets(targetRtv, null);
            cmdList.DrawInstanced(3, 1, 0, 0);
        }
    }
}

using System.Diagnostics;
using Vortice.Direct3D;
using Vortice.Direct3D12;

namespace Direct3D12
{
    static class D3D12PostProcess
    {
        enum FxRootParamIndices
        {
            RootConstants,
            DescriptorTable,
        }
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
            var range = D3D12Helpers.Range(
                DescriptorRangeType.ShaderResourceView,
                D3D12.DescriptorRangeOffsetAppend,
                0,
                0,
                DescriptorRangeFlags.DescriptorsVolatile);

            //Get the number of items in the FxRootParamIndices enum
            RootParameter1[] parameters =
            [
                D3D12Helpers.AsConstants(1, ShaderVisibility.Pixel, 1),
                D3D12Helpers.AsDescriptorTable(ShaderVisibility.Pixel, [range]),
            ];

            var rootSignature = D3D12Helpers.AsRootSignatureDesc(parameters);
            fxRootSig = D3D12Helpers.CreateRootSignature(D3D12Graphics.Device, rootSignature);
            Debug.Assert(fxRootSig != null);
            D3D12Helpers.NameD3D12Object(fxRootSig, "Post-process FX Root Signature");

            // Create FX PSO
            PipelineStateStream data = new()
            {
                RootSignature = new PipelineStateSubObjectTypeRootSignature(fxRootSig),
                Vs = new(D3D12Shaders.GetEngineShader(EngineShaders.FullScreenTriangleVs)),
                Ps = new(D3D12Shaders.GetEngineShader(EngineShaders.PostProcessPs)),
                PrimitiveTopology = new(PrimitiveTopologyType.Triangle),
                RenderTargetFormats = new([D3D12Surface.DefaultBackBufferFormat]),
                Rasterizer = new(D3D12Helpers.RasterizerStatesCollection.NoCull),
            };

            fxPso = D3D12Graphics.Device.CreatePipelineState(data);
            D3D12Helpers.NameD3D12Object(fxPso, "Post-process FX Pipeline State Object");

            return fxRootSig != null && fxPso != null;
        }

        public static void Shutdown()
        {
            fxRootSig?.Release();
            fxPso?.Release();
        }

        public static void PostProcess(ID3D12GraphicsCommandList cmdList, CpuDescriptorHandle targetRtv)
        {
            cmdList.SetGraphicsRootSignature(fxRootSig);
            cmdList.SetPipelineState(fxPso);

            cmdList.SetGraphicsRoot32BitConstant((int)FxRootParamIndices.RootConstants, D3D12GPass.MainBuffer.Srv.Index, 0);
            cmdList.SetGraphicsRootDescriptorTable((int)FxRootParamIndices.DescriptorTable, D3D12Graphics.SrvHeap.GpuStart);
            cmdList.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            // NOTE: we don't need to clear the render target, because each pixel will 
            //       be overwritten by pixels from gpass main buffer.
            //       We also don't need a depth buffer.
            cmdList.OMSetRenderTargets(targetRtv, null);
            cmdList.DrawInstanced(3, 1, 0, 0);
        }
    }
}

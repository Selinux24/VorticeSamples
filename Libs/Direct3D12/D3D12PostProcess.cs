﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
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
        struct FxPsoData
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

        private static bool CreateFxPsoAndRootSignature(D3D12Graphics graphics)
        {
            Debug.Assert(fxRootSig == null && fxPso == null);
            // Create FX root signature
            DescriptorRange1 range = new()
            {
                RangeType = DescriptorRangeType.ShaderResourceView,
                OffsetInDescriptorsFromTableStart = D3D12.DescriptorRangeOffsetAppend,
                NumDescriptors = 0,
                RegisterSpace = 0,
                BaseShaderRegister = 0,
                Flags = DescriptorRangeFlags.DescriptorsVolatile,
            };

            //Get the number of items in the FxRootParamIndices enum
            int numRootParams = Enum.GetValues(typeof(FxRootParamIndices)).Length;
            RootParameter1[] parameters = new RootParameter1[numRootParams];
            parameters[(int)FxRootParamIndices.RootConstants] = D3D12Helpers.AsConstants(1, ShaderVisibility.Pixel, 1);
            parameters[(int)FxRootParamIndices.DescriptorTable] = D3D12Helpers.AsDescriptorTable(ShaderVisibility.Pixel, [range]);

            RootSignatureDescription1 rootSignature = new() { Parameters = parameters };
            fxRootSig = D3D12Helpers.CreateRootSignature(graphics.Device, rootSignature);
            Debug.Assert(fxRootSig != null);
            fxRootSig.Name = "Post-process FX Root Signature";

            // Create FX PSO
            FxPsoData data = new()
            {
                RootSignature = new PipelineStateSubObjectTypeRootSignature(fxRootSig),
                Vs = new(D3D12Shaders.GetEngineShader(EngineShaders.FullScreenTriangleVs)),
                Ps = new(D3D12Shaders.GetEngineShader(EngineShaders.PostProcessPs)),
                PrimitiveTopology = new(PrimitiveTopologyType.Triangle),
                RenderTargetFormats = new([D3D12Surface.DefaultBackBufferFormat]),
                Rasterizer = new(D3D12Helpers.RasterizerState.NoCull),
            };

            IntPtr stream = IntPtr.Zero;
            Marshal.StructureToPtr(data, stream, false);
            int streamSize = Marshal.SizeOf(data);

            fxPso = D3D12Helpers.CreatePipelineState(graphics.Device, stream, streamSize);
            fxPso.Name = "Post-process FX Pipeline State Object";

            return fxRootSig != null && fxPso != null;
        }

        public static bool Initialize(D3D12Graphics graphics)
        {
            return CreateFxPsoAndRootSignature(graphics);
        }

        public static void Shutdown()
        {
            fxRootSig?.Release();
            fxPso?.Release();
        }

        public static void PostProcess(D3D12Graphics graphics, ID3D12GraphicsCommandList cmdList, CpuDescriptorHandle targetRtv)
        {
            cmdList.SetGraphicsRootSignature(fxRootSig);
            cmdList.SetPipelineState(fxPso);

            cmdList.SetGraphicsRoot32BitConstant((int)FxRootParamIndices.RootConstants, D3D12GPass.MainBuffer.Srv.Index, 0);
            cmdList.SetGraphicsRootDescriptorTable((int)FxRootParamIndices.DescriptorTable, graphics.SrvHeap.GpuStart);
            cmdList.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            // NOTE: we don't need to clear the render target, because each pixel will 
            //       be overwritten by pixels from gpass main buffer.
            //       We also don't need a depth buffer.
            cmdList.OMSetRenderTargets(targetRtv, null);
            cmdList.DrawInstanced(3, 1, 0, 0);
        }
    }
}
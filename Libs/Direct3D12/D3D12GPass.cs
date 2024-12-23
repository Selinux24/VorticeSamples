﻿using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Direct3D12
{
    static class D3D12GPass
    {
        enum GPassRootParamIndices : uint
        {
            RootConstants = 0
        }
        struct PipelineStateStream
        {
            public PipelineStateSubObjectTypeRootSignature RootSignature;
            public PipelineStateSubObjectTypeVertexShader Vs;
            public PipelineStateSubObjectTypePixelShader Ps;
            public PipelineStateSubObjectTypePrimitiveTopology PrimitiveTopology;
            public PipelineStateSubObjectTypeRenderTargetFormats RenderTargetFormats;
            public PipelineStateSubObjectTypeDepthStencilFormat DepthStencilFormat;
            public PipelineStateSubObjectTypeRasterizer Rasterizer;
            public PipelineStateSubObjectTypeDepthStencil1 Depth;
        }

        const Format mainBufferFormat = Format.R16G16B16A16_Float;
        const Format depthBufferFormat = Format.D32_Float;
        static readonly SizeI initialDimensions = new() { Width = 100, Height = 100 };

        static D3D12RenderTexture gpassMainBuffer;
        static D3D12DepthBuffer gpassDepthBuffer;
        static SizeI dimensions = initialDimensions;
        static ResourceBarrierFlags flags = ResourceBarrierFlags.None;

        static ID3D12RootSignature gpassRootSig = null;
        static ID3D12PipelineState gpassPso = null;

#if DEBUG
        static readonly Color clearValue = new(0.5f, 0.5f, 0.5f, 1.0f);
#else
        static readonly Color clearValue = new(0.0f);
#endif

        public static D3D12RenderTexture MainBuffer { get => gpassMainBuffer; }
        public static D3D12DepthBuffer DepthBuffer { get => gpassDepthBuffer; }

        public static bool Initialize()
        {
            return
                CreateBuffers(initialDimensions) &&
                CreateGPassPsoAndRootSignature();
        }
        private static bool CreateBuffers(SizeI size)
        {
            Debug.Assert(size.Width > 0 && size.Height > 0);
            gpassMainBuffer?.Release();
            gpassDepthBuffer?.Release();

            // Create the main buffer
            {
                ResourceDescription desc = new()
                {
                    Alignment = 0, // NOTE: 0 is the same as 64KB (or 4MB for MSAA)
                    DepthOrArraySize = 1,
                    Dimension = ResourceDimension.Texture2D,
                    Flags = ResourceFlags.AllowRenderTarget,
                    Format = mainBufferFormat,
                    Height = size.Height,
                    Layout = TextureLayout.Unknown,
                    MipLevels = 0, // make space for all mip levels
                    SampleDescription = new(1, 0),
                    Width = (ulong)size.Width
                };

                D3D12TextureInitInfo info = new()
                {
                    Desc = desc,
                    InitialState = ResourceStates.PixelShaderResource,
                    ClearValue = new()
                    {
                        Format = desc.Format,
                        Color = clearValue,
                    }
                };

                gpassMainBuffer = new D3D12RenderTexture(info);
            }

            // Create the depth buffer
            {
                ResourceDescription desc = new()
                {
                    Alignment = 0, // NOTE: 0 is the same as 64KB (or 4MB for MSAA)
                    DepthOrArraySize = 1,
                    Dimension = ResourceDimension.Texture2D,
                    Flags = ResourceFlags.AllowDepthStencil,
                    Format = depthBufferFormat,
                    Height = size.Height,
                    Layout = TextureLayout.Unknown,
                    MipLevels = 1,
                    SampleDescription = new(1, 0),
                    Width = (ulong)size.Width
                };

                D3D12TextureInitInfo info = new()
                {
                    Desc = desc,
                    InitialState = ResourceStates.DepthRead | ResourceStates.PixelShaderResource | ResourceStates.NonPixelShaderResource,
                    ClearValue = new()
                    {
                        Format = desc.Format,
                        Color = clearValue,
                        DepthStencil = new(0f, 0),
                    }
                };

                gpassDepthBuffer = new D3D12DepthBuffer(info);
            }

            gpassMainBuffer.Resource.Name = "GPass Main Buffer";
            gpassDepthBuffer.Resource.Name = "GPass Depth Buffer";

            flags = ResourceBarrierFlags.None;

            return gpassMainBuffer.Resource != null && gpassDepthBuffer.Resource != null;
        }
        private static bool CreateGPassPsoAndRootSignature()
        {
            Debug.Assert(gpassRootSig == null && gpassPso == null);

            // Create GPass root signature
            int numRootParams = Enum.GetValues(typeof(GPassRootParamIndices)).Length;
            RootParameter1[] parameters =
            [
                D3D12Helpers.AsConstants(3, ShaderVisibility.Pixel, numRootParams)
            ];

            var rootSignature = D3D12Helpers.AsRootSignatureDesc(parameters);
            gpassRootSig = D3D12Helpers.CreateRootSignature(D3D12Graphics.Device, rootSignature);
            Debug.Assert(gpassRootSig != null);
            gpassRootSig.Name = "GPass Root Signature";

            Format[] formats = [mainBufferFormat];

            // Create GPass PSO
            PipelineStateStream pipelineState = new();
            {
                pipelineState.RootSignature = gpassRootSig;
                pipelineState.Vs = new(D3D12Shaders.GetEngineShader(EngineShaders.FullScreenTriangleVs));
                pipelineState.Ps = new(D3D12Shaders.GetEngineShader(EngineShaders.FillColorPs));
                pipelineState.PrimitiveTopology = new(PrimitiveTopologyType.Triangle);
                pipelineState.RenderTargetFormats = new(formats);
                pipelineState.DepthStencilFormat = new(depthBufferFormat);
                pipelineState.Rasterizer = new(D3D12Helpers.RasterizerState.NoCull);
                pipelineState.Depth = new(D3D12Helpers.DepthState.Disabled);
            }

            IntPtr stream = IntPtr.Zero;
            Marshal.StructureToPtr(pipelineState, stream, false);
            int streamSize = Marshal.SizeOf(pipelineState);

            gpassPso = D3D12Helpers.CreatePipelineState(D3D12Graphics.Device, stream, streamSize);
            gpassPso.Name = "GPass Pipeline State Object";

            return gpassRootSig != null && gpassPso != null;
        }

        public static void Shutdown()
        {
            gpassMainBuffer.Release();
            gpassDepthBuffer.Release();
            dimensions = initialDimensions;

            gpassRootSig.Release();
            gpassPso.Release();
        }

        public static void SetSize(SizeI size)
        {
            var d = dimensions;
            if (size.Width <= d.Width && size.Height <= d.Height)
            {
                return;
            }

            d = new()
            {
                Width = Math.Max(size.Width, d.Width),
                Height = Math.Max(size.Height, d.Height)
            };

            CreateBuffers(d);
        }

        public static void DepthPrePass(ID3D12GraphicsCommandList cmdList, D3D12FrameInfo info)
        {

        }

        struct FrameConstants
        {
            public float Width;
            public float Height;
            public int Frame;
        }

        public static void Render(ID3D12GraphicsCommandList cmdList, D3D12FrameInfo info)
        {
            cmdList.SetGraphicsRootSignature(gpassRootSig);
            cmdList.SetPipelineState(gpassPso);

            int frame = 0;
            FrameConstants constants = new()
            {
                Width = info.SurfaceWidth,
                Height = info.SurfaceHeight,
                Frame = ++frame,
            };
            cmdList.SetGraphicsRoot32BitConstants((int)GPassRootParamIndices.RootConstants, constants, 0);

            cmdList.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            cmdList.DrawInstanced(3, 1, 0, 0);
        }
        public static void AddTransitionsForDepthPrePass(D3D12ResourceBarrier barriers)
        {
            barriers.Add(
                gpassMainBuffer.Resource,
                ResourceStates.PixelShaderResource,
                ResourceStates.RenderTarget,
                ResourceBarrierFlags.BeginOnly);

            barriers.Add(
                gpassDepthBuffer.Resource,
                ResourceStates.DepthRead |
                ResourceStates.PixelShaderResource |
                ResourceStates.NonPixelShaderResource,
                ResourceStates.DepthWrite,
                flags);

            flags = ResourceBarrierFlags.EndOnly;
        }
        public static void AddTransitionsForGPass(D3D12ResourceBarrier barriers)
        {
            barriers.Add(
                gpassMainBuffer.Resource,
                ResourceStates.PixelShaderResource,
                ResourceStates.RenderTarget,
                ResourceBarrierFlags.EndOnly);

            barriers.Add(
                gpassDepthBuffer.Resource,
                ResourceStates.DepthWrite,
                ResourceStates.DepthRead |
                ResourceStates.PixelShaderResource |
                ResourceStates.NonPixelShaderResource);
        }
        public static void AddTransitionsForPostProcess(D3D12ResourceBarrier barriers)
        {
            barriers.Add(
                gpassMainBuffer.Resource,
                ResourceStates.RenderTarget,
                ResourceStates.PixelShaderResource);

            barriers.Add(
                gpassDepthBuffer.Resource,
                ResourceStates.DepthRead | ResourceStates.PixelShaderResource | ResourceStates.NonPixelShaderResource,
                ResourceStates.DepthWrite,
                ResourceBarrierFlags.BeginOnly);
        }
        public static void SetRenderTargetsForDepthPrePass(ID3D12GraphicsCommandList cmdList)
        {
            var dsv = gpassDepthBuffer.Dsv;
            cmdList.ClearDepthStencilView(dsv.Cpu, ClearFlags.Depth | ClearFlags.Stencil, 0f, 0, 0, null);
            cmdList.OMSetRenderTargets([], dsv.Cpu);
        }
        public static void SetRenderTargetsForGPass(ID3D12GraphicsCommandList cmdList)
        {
            var rtv = gpassMainBuffer.GetRtv(0);
            var dsv = gpassDepthBuffer.Dsv;

            cmdList.ClearRenderTargetView(rtv, clearValue, 0, null);
            cmdList.OMSetRenderTargets(rtv, dsv.Cpu);
        }
    }
}

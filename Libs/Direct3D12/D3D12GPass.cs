using System;
using System.Diagnostics;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Direct3D12
{
    static class D3D12GPass
    {
        private const int RP_Count = 1;
        private const int RP_RootConstants = 0;

        struct FrameConstants
        {
            public float Width;
            public float Height;
            public int Frame;
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

        static FrameConstants frameConstants = new();

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
            gpassMainBuffer?.Dispose();
            gpassDepthBuffer?.Dispose();

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
                    ClearValue = new(desc.Format, clearValue),
                };

                gpassMainBuffer = new D3D12RenderTexture(info);
            }

            // Create the depth buffer
            {
                var desc = ResourceDescription.Texture2D(
                    depthBufferFormat,
                    (uint)size.Width,
                    (uint)size.Height,
                    1,
                    1,
                    flags: ResourceFlags.AllowDepthStencil);

                D3D12TextureInitInfo info = new()
                {
                    Desc = desc,
                    InitialState = ResourceStates.DepthRead | ResourceStates.PixelShaderResource | ResourceStates.NonPixelShaderResource,
                    ClearValue = new(desc.Format, 0f, 0),
                };

                gpassDepthBuffer = new D3D12DepthBuffer(info);
            }

            D3D12Helpers.NameD3D12Object(gpassMainBuffer.Resource, "GPass Main Buffer");
            D3D12Helpers.NameD3D12Object(gpassDepthBuffer.Resource, "GPass Depth Buffer");

            flags = ResourceBarrierFlags.None;

            return gpassMainBuffer.Resource != null && gpassDepthBuffer.Resource != null;
        }
        private static bool CreateGPassPsoAndRootSignature()
        {
            Debug.Assert(gpassRootSig == null && gpassPso == null);

            // Create GPass root signature
            var parameters = new RootParameter1[RP_Count];
            parameters[RP_RootConstants] = D3D12Helpers.AsConstants(3, ShaderVisibility.Pixel, 1);

            var rootSignature = D3D12Helpers.AsRootSignatureDesc(parameters);
            gpassRootSig = D3D12Helpers.CreateRootSignature(D3D12Graphics.Device, rootSignature);
            Debug.Assert(gpassRootSig != null);
            D3D12Helpers.NameD3D12Object(gpassRootSig, "GPass Root Signature");

            // Create GPass PSO
            PipelineStateStream pipelineState = new();
            {
                pipelineState.RootSignature = gpassRootSig;
                pipelineState.Vs = new(D3D12Shaders.GetEngineShader(EngineShaders.FullScreenTriangleVs));
                pipelineState.Ps = new(D3D12Shaders.GetEngineShader(EngineShaders.FillColorPs));
                pipelineState.PrimitiveTopology = new(PrimitiveTopologyType.Triangle);
                pipelineState.RenderTargetFormats = new([mainBufferFormat]);
                pipelineState.DepthStencilFormat = new(depthBufferFormat);
                pipelineState.Rasterizer = new(D3D12Helpers.RasterizerStatesCollection.NoCull);
                pipelineState.Depth = new(D3D12Helpers.DepthStatesCollection.Disabled);
            }

            gpassPso = D3D12Graphics.Device.CreatePipelineState(pipelineState);
            D3D12Helpers.NameD3D12Object(gpassPso, "GPass Pipeline State Object");

            return gpassRootSig != null && gpassPso != null;
        }

        public static void Shutdown()
        {
            gpassMainBuffer.Dispose();
            gpassMainBuffer = null;
            gpassDepthBuffer.Dispose();
            gpassDepthBuffer = null;
            dimensions = initialDimensions;

            gpassRootSig.Dispose();
            gpassRootSig = null;
            gpassPso.Dispose();
            gpassPso = null;
        }

        public static void SetSize(SizeI size)
        {
            if (size.Width <= dimensions.Width && size.Height <= dimensions.Height)
            {
                return;
            }

            dimensions.Width = Math.Max(size.Width, dimensions.Width);
            dimensions.Height = Math.Max(size.Height, dimensions.Height);

            CreateBuffers(dimensions);
        }

        public static void DepthPrePass(ID3D12GraphicsCommandList cmdList, D3D12FrameInfo info)
        {

        }

        public static void Render(ID3D12GraphicsCommandList cmdList, D3D12FrameInfo info)
        {
            cmdList.SetGraphicsRootSignature(gpassRootSig);
            cmdList.SetPipelineState(gpassPso);

            frameConstants.Width = info.SurfaceWidth;
            frameConstants.Height = info.SurfaceHeight;
            frameConstants.Frame++;
            cmdList.SetGraphicsRoot32BitConstants(RP_RootConstants, frameConstants, 0);

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

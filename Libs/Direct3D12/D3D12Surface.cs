using Direct3D12.Delight;
using PrimalLike.Common;
using PrimalLike.EngineAPI;
using System;
using System.Diagnostics;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Direct3D12
{
    /// <summary>
    /// Represents a D3D12 surface.
    /// </summary>
    class D3D12Surface : IDisposable
    {
        const int BufferCount = 3;

        struct RenderTargetData
        {
            public ID3D12Resource Resource;
            public DescriptorHandle Rtv;
        }

        IDXGISwapChain4 swapChain;
        readonly RenderTargetData[] renderTargetData = new RenderTargetData[BufferCount];
        readonly Window window;
        uint currentBbIndex = 0;
        Viewport viewport;
        RectI scissorRect;
        uint lightCullingId = uint.MaxValue;

        /// <summary>
        /// Gets the width of the surface.
        /// </summary>
        public uint Width { get => (uint)viewport.Width; }
        /// <summary>
        /// Gets the height of the surface.
        /// </summary>
        public uint Height { get => (uint)viewport.Height; }
        /// <summary>
        /// Gets the light culling id.
        /// </summary>
        public uint LightCullingId { get => lightCullingId; }
        /// <summary>
        /// Gets the current render target backbuffer resource.
        /// </summary>
        public ID3D12Resource GetBackbuffer() { return renderTargetData[currentBbIndex].Resource; }
        /// <summary>
        /// Gets the current render target view descriptor handle.
        /// </summary>
        public DescriptorHandle GetRtv() { return renderTargetData[currentBbIndex].Rtv; }
        /// <summary>
        /// Gets the viewport.
        /// </summary>
        public Viewport GetViewport() { return viewport; }
        /// <summary>
        /// Gets the scissor rectangle.
        /// </summary>
        public RectI GetScissorRect() { return scissorRect; }

        /// <summary>
        /// Creates a new instance of the <see cref="D3D12Surface"/> class.
        /// </summary>
        /// <param name="window">Platform window</param>
        public D3D12Surface(Window window)
        {
            Debug.Assert(window != null && window.Handle != 0);
            this.window = window;
        }
        /// <summary>
        /// Finalizes an instance of the <see cref="D3D12Surface"/> class.
        /// </summary>
        ~D3D12Surface()
        {
            Dispose(false);
        }
        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        void Dispose(bool disposing)
        {
            if (disposing)
            {
                Release();
            }
        }
        void Release()
        {
            if (IdDetail.IsValid(lightCullingId))
            {
                D3D12LightCulling.RemoveCuller(lightCullingId);
            }

            for (int i = 0; i < BufferCount; i++)
            {
                renderTargetData[i].Resource?.Dispose();
                renderTargetData[i].Resource = null;

                D3D12Graphics.RtvHeap.Free(ref renderTargetData[i].Rtv);
            }

            swapChain?.Dispose();
            swapChain = null;
        }

        static Format ToNonSrgb(Format format)
        {
            if (format == Format.R8G8B8A8_UNorm_SRgb)
            {
                return Format.R8G8B8A8_UNorm;
            }

            return format;
        }
        static SwapChainFlags GetFlags()
        {
            return D3D12Graphics.AllowTearing() ? SwapChainFlags.AllowTearing : SwapChainFlags.None;
        }

        /// <summary>
        /// Creates a swap chain.
        /// </summary>
        /// <param name="factory">Swap chain factory</param>
        /// <param name="cmdQueue">Command queue</param>
        /// <param name="format">Swap chain format</param>
        public void CreateSwapChain(IDXGIFactory7 factory, ID3D12CommandQueue cmdQueue)
        {
            Debug.Assert(factory != null && cmdQueue != null);
            Release();

            uint frameBufferCount = BufferCount;

            SwapChainDescription1 desc = new()
            {
                AlphaMode = AlphaMode.Unspecified,
                BufferCount = frameBufferCount,
                BufferUsage = Usage.RenderTargetOutput,
                Flags = GetFlags(),
                Format = ToNonSrgb(D3D12Graphics.DefaultBackBufferFormat),
                Height = window.Height,
                Width = window.Width,
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.FlipDiscard,
                Stereo = false,
                SampleDescription = new(1, 0)
            };

            var hwnd = window.Handle;
            var sc = factory.CreateSwapChainForHwnd(cmdQueue, hwnd, desc);
            factory.MakeWindowAssociation(hwnd, WindowAssociationFlags.IgnoreAltEnter);
            swapChain = sc.QueryInterface<IDXGISwapChain4>();
            sc.Dispose();

            currentBbIndex = swapChain.CurrentBackBufferIndex;

            for (int i = 0; i < frameBufferCount; i++)
            {
                renderTargetData[i].Rtv = D3D12Graphics.RtvHeap.Allocate();
            }

            FinalizeSwapChainCreation();

            Debug.Assert(!IdDetail.IsValid(lightCullingId));
            lightCullingId = D3D12LightCulling.AddCuller();
        }
        /// <summary>
        /// Finishes the swap chain creation.
        /// </summary>
        void FinalizeSwapChainCreation()
        {
            // create RTVs for back-buffers
            for (uint i = 0; i < BufferCount; i++)
            {
                Debug.Assert(renderTargetData[i].Resource == null);
                swapChain.GetBuffer(i, out renderTargetData[i].Resource);
                RenderTargetViewDescription rtvdesc = new()
                {
                    Format = D3D12Graphics.DefaultBackBufferFormat,
                    ViewDimension = RenderTargetViewDimension.Texture2D
                };
                D3D12Graphics.Device.CreateRenderTargetView(renderTargetData[i].Resource, rtvdesc, renderTargetData[i].Rtv.Cpu);
            }

            var scdesc = swapChain.Description;
            uint width = scdesc.BufferDescription.Width;
            uint height = scdesc.BufferDescription.Height;
            Debug.Assert(window.Width == width && window.Height == height);

            // set viewport and scissor rect
            viewport = new(width, height);
            scissorRect = new(0, 0, (int)width, (int)height);
        }

        /// <summary>
        /// Presents the surface.
        /// </summary>
        public void Present()
        {
            Debug.Assert(swapChain != null);

            uint syncInterval = 0u;
            var presentFlags = PresentFlags.AllowTearing;
            if (D3D12Graphics.VSyncEnabled())
            {
                syncInterval = 1u;
                presentFlags = PresentFlags.None;
            }

            swapChain.Present(syncInterval, presentFlags);
            currentBbIndex = swapChain.CurrentBackBufferIndex;
        }
        /// <summary>
        /// Resizes the surface.
        /// </summary>
        public void Resize()
        {
            Debug.Assert(swapChain != null);

            for (int i = 0; i < BufferCount; i++)
            {
                renderTargetData[i].Resource?.Dispose();
                renderTargetData[i].Resource = null;
            }

            SwapChainFlags flags = GetFlags();
            swapChain.ResizeBuffers(BufferCount, 0, 0, Format.Unknown, flags);
            currentBbIndex = swapChain.CurrentBackBufferIndex;

            FinalizeSwapChainCreation();

            Debug.WriteLine("D3D12 Surface Resized.");
        }
    }
}

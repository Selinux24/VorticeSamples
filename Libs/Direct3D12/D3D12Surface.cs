﻿using PrimalLike.Graphics;
using PrimalLike.Platform;
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
    class D3D12Surface : ISurface
    {
        public const Format DefaultBackBufferFormat = Format.R8G8B8A8_UNorm_SRgb;
        const int BufferCount = 3;

        struct RenderTargetData
        {
            public ID3D12Resource Resource;
            public D3D12DescriptorHandle Rtv;
        }

        private IDXGISwapChain4 swapChain;
        private readonly RenderTargetData[] renderTargetData = new RenderTargetData[BufferCount];
        private readonly PlatformWindow window;
        private int currentBbIndex = 0;
        private readonly bool allowTearing = false;
        private PresentFlags presentFlags = 0;
        private Viewport viewport;
        private RectI scissorRect;
        private Format format = DefaultBackBufferFormat;

        /// <inheritdoc/>
        public uint Id { get; set; }
        /// <inheritdoc/>
        public int Width { get => (int)viewport.Width; }
        /// <inheritdoc/>
        public int Height { get => (int)viewport.Height; }
        public ID3D12Resource Backbuffer { get => renderTargetData[currentBbIndex].Resource; }
        public CpuDescriptorHandle Rtv { get => renderTargetData[currentBbIndex].Rtv.Cpu; }
        public Viewport Viewport { get => viewport; }
        public RectI ScissorRect { get => scissorRect; }

        /// <summary>
        /// Creates a new instance of the <see cref="D3D12Surface"/> class.
        /// </summary>
        /// <param name="window">Platform window</param>
        public D3D12Surface(PlatformWindow window)
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
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                Release();
            }
        }
        private void Release()
        {
            for (int i = 0; i < BufferCount; i++)
            {
                renderTargetData[i].Resource?.Dispose();
                renderTargetData[i].Resource = null;

                D3D12Graphics.RtvHeap.Free(ref renderTargetData[i].Rtv);
            }

            swapChain?.Dispose();
            swapChain = null;
        }

        private static Format ToNonSrgb(Format format)
        {
            if (format == Format.R8G8B8A8_UNorm_SRgb)
            {
                return Format.R8G8B8A8_UNorm;
            }

            return format;
        }

        /// <summary>
        /// Creates a swap chain.
        /// </summary>
        /// <param name="factory">Swap chain factory</param>
        /// <param name="cmdQueue">Command queue</param>
        /// <param name="format">Swap chain format</param>
        public void CreateSwapChain(IDXGIFactory7 factory, ID3D12CommandQueue cmdQueue, Format format = DefaultBackBufferFormat)
        {
            Debug.Assert(factory != null && cmdQueue != null);
            Release();

            int frameBufferCount = BufferCount;

            if (factory.CheckFeatureSupport(Vortice.DXGI.Feature.PresentAllowTearing, allowTearing) && allowTearing)
            {
                presentFlags = PresentFlags.AllowTearing;
            }

            this.format = format;

            SwapChainDescription1 desc = new()
            {
                AlphaMode = AlphaMode.Unspecified,
                BufferCount = frameBufferCount,
                BufferUsage = Usage.RenderTargetOutput,
                Flags = allowTearing ? SwapChainFlags.AllowTearing : 0,
                Format = ToNonSrgb(format),
                Height = window.Height,
                Width = window.Width,
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.FlipDiscard,
                Stereo = false
            };
            desc.SampleDescription.Count = 1;
            desc.SampleDescription.Quality = 0;

            IntPtr hwnd = window.Handle;
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
        }
        /// <summary>
        /// Finishes the swap chain creation.
        /// </summary>
        private void FinalizeSwapChainCreation()
        {
            // create RTVs for back-buffers
            for (int i = 0; i < BufferCount; i++)
            {
                Debug.Assert(renderTargetData[i].Resource == null);
                swapChain.GetBuffer(i, out renderTargetData[i].Resource);
                RenderTargetViewDescription rtvdesc = new()
                {
                    Format = format,
                    ViewDimension = RenderTargetViewDimension.Texture2D
                };
                D3D12Graphics.Device.CreateRenderTargetView(renderTargetData[i].Resource, rtvdesc, renderTargetData[i].Rtv.Cpu);
            }

            SwapChainDescription scdesc = swapChain.Description;
            int width = scdesc.BufferDescription.Width;
            int height = scdesc.BufferDescription.Height;
            Debug.Assert(window.Width == width && window.Height == height);

            // set viewport and scissor rect
            viewport.X = 0f;
            viewport.Y = 0f;
            viewport.Width = width;
            viewport.Height = height;
            viewport.MinDepth = 0f;
            viewport.MaxDepth = 1f;

            scissorRect = new(0, 0, width, height);
        }

        /// <summary>
        /// Presents the surface.
        /// </summary>
        public void Present()
        {
            Debug.Assert(swapChain != null);
            swapChain.Present(0, presentFlags);
            currentBbIndex = swapChain.CurrentBackBufferIndex;
        }

        /// <inheritdoc/>
        public void Resize(int width, int height)
        {
            int frameBufferCount = BufferCount;

            Debug.Assert(swapChain != null);
            for (int i = 0; i < frameBufferCount; i++)
            {
                renderTargetData[i].Resource?.Dispose();
                renderTargetData[i].Resource = null;
            }

            SwapChainFlags flags = allowTearing ? SwapChainFlags.AllowTearing : 0;
            swapChain.ResizeBuffers(frameBufferCount, 0, 0, Format.Unknown, flags);
            currentBbIndex = swapChain.CurrentBackBufferIndex;

            FinalizeSwapChainCreation();

            Debug.WriteLine("D3D12 Surface Resized.");
        }
    }
}

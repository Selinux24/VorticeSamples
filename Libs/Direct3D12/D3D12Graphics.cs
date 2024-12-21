using Engine.Graphics;
using Engine.Platform;
using SharpGen.Runtime;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.DXGI;

#if DEBUG
using Vortice.Direct3D12.Debug;
#endif

namespace Direct3D12
{
    /// <summary>
    /// D3D12 graphics implementation.
    /// </summary>
    class D3D12Graphics : IPlatform
    {
        private const FeatureLevel MinimumFeatureLevel = FeatureLevel.Level_11_0;
        private const string EngineShaderPaths = "Content/engineShaders.bin";

        private D3D12Device mainDevice;
#if DEBUG
        private ID3D12InfoQueue infoQueue;
#endif
        private IDXGIFactory7 dxgiFactory;
        private D3D12Command gfxCommand;
        private readonly List<D3D12Surface> surfaces = [];

        private readonly D3D12DescriptorHeap rtvDescHeap;
        private readonly D3D12DescriptorHeap dsvDescHeap;
        private readonly D3D12DescriptorHeap srvDescHeap;
        private readonly D3D12DescriptorHeap uavDescHeap;

        private readonly List<IUnknown>[] deferredReleases;
        private readonly int[] deferredReleasesFlags;
        private readonly Mutex deferredReleasesMutex;

        /// <summary>
        /// Gets or sets the number of frame buffers.
        /// </summary>
        public int FrameBufferCount { get; } = 3;
        /// <summary>
        /// Gets the main D3D12 device.
        /// </summary>
        public D3D12Device Device { get => mainDevice; }
        /// <summary>
        /// Gets the RTV descriptor heap.
        /// </summary>
        public D3D12DescriptorHeap RtvHeap { get => rtvDescHeap; }
        /// <summary>
        /// Gets the DSV descriptor heap.
        /// </summary>
        public D3D12DescriptorHeap DsvHeap { get => dsvDescHeap; }
        /// <summary>
        /// Gets the SRV descriptor heap.
        /// </summary>
        public D3D12DescriptorHeap SrvHeap { get => srvDescHeap; }
        /// <summary>
        /// Gets the UAV descriptor heap.
        /// </summary>
        public D3D12DescriptorHeap UavHeap { get => uavDescHeap; }
        /// <summary>
        /// Gets the current frame index.
        /// </summary>
        public int CurrentFrameIndex { get => gfxCommand.FrameIndex; }

        /// <summary>
        /// Constructs a new instance of <see cref="D3D12Graphics"/>.
        /// </summary>
        public D3D12Graphics()
        {
            rtvDescHeap = new(this, DescriptorHeapType.RenderTargetView);
            dsvDescHeap = new(this, DescriptorHeapType.DepthStencilView);
            srvDescHeap = new(this, DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
            uavDescHeap = new(this, DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);

            deferredReleases = new List<IUnknown>[FrameBufferCount];
            for (int i = 0; i < FrameBufferCount; i++)
            {
                deferredReleases[i] = [];
            }
            deferredReleasesFlags = new int[FrameBufferCount];
            deferredReleasesMutex = new();
        }

        /// <summary>
        /// Sets the deferred releases flag.
        /// </summary>
        public void SetDeferredReleasesFlag()
        {
            deferredReleasesFlags[CurrentFrameIndex] = 1;
        }

        /// <inheritdoc/>
        public bool Initialize()
        {
            if (mainDevice != null)
            {
                Shutdown();
            }

            bool debug = false;
#if DEBUG
            {
                if (D3D12.D3D12GetDebugInterface<ID3D12Debug>(out var debugInterface).Success)
                {
                    debugInterface.EnableDebugLayer();
                    debugInterface.Dispose();
                }
                else
                {
                    Debug.WriteLine("Warning: D3D12 Debug interface is not available. Verify that Graphics Tools optional feature is installed on this system.");
                }
                debug = true;

                if (D3D12.D3D12GetDebugInterface(out ID3D12DeviceRemovedExtendedDataSettings1 dredSettings).Success)
                {
                    // Turn on auto-breadcrumbs and page fault reporting.
                    dredSettings.SetAutoBreadcrumbsEnablement(DredEnablement.ForcedOn);
                    dredSettings.SetPageFaultEnablement(DredEnablement.ForcedOn);
                    dredSettings.SetBreadcrumbContextEnablement(DredEnablement.ForcedOn);
                    dredSettings.Dispose();
                }
            }
#endif
            if (!DXGI.CreateDXGIFactory2(debug, out dxgiFactory).Success)
            {
                return FailedInit();
            }

            var mainAdapter = DetermineMainAdapter();
            if (mainAdapter == null)
            {
                return FailedInit();
            }

            var maxFeatureLevel = GetMaxFeatureLevel(mainAdapter);
            Debug.Assert(maxFeatureLevel >= MinimumFeatureLevel);
            if (maxFeatureLevel < MinimumFeatureLevel)
            {
                return FailedInit();
            }

            if (!D3D12.D3D12CreateDevice(mainAdapter, maxFeatureLevel, out mainDevice).Success)
            {
                return FailedInit();
            }
            mainDevice.Name = "Main D3D12 Device";

            gfxCommand = new D3D12Command(this, CommandListType.Direct);
            if (gfxCommand.CommandQueue == null)
            {
                return FailedInit();
            }

#if DEBUG
            {
                var iq = mainDevice.QueryInterfaceOrNull<ID3D12InfoQueue1>();
                if (iq != null)
                {
                    iq.RegisterMessageCallback(DebugCallback, MessageCallbackFlags.None);
                    infoQueue = iq;
                }
                else
                {
                    infoQueue = mainDevice.QueryInterfaceOrNull<ID3D12InfoQueue>();
                }
            }
#endif

            bool result = true;
            result &= rtvDescHeap.Initialize(512, false);
            result &= dsvDescHeap.Initialize(512, false);
            result &= srvDescHeap.Initialize(4096, true);
            result &= uavDescHeap.Initialize(512, false);
            if (!result)
            {
                return FailedInit();
            }

            gfxCommand = new(this, CommandListType.Direct);
            if (gfxCommand.CommandQueue == null)
            {
                return FailedInit();
            }

            // initialize modules
            if (!D3D12Shaders.Initialize())
            {
                return FailedInit();
            }

            mainDevice.Name = "Main D3D12 Device";
            rtvDescHeap.Heap.Name = "RTV Descriptor Heap";
            dsvDescHeap.Heap.Name = "DSV Descriptor Heap";
            srvDescHeap.Heap.Name = "SRV Descriptor Heap";
            uavDescHeap.Heap.Name = "UAV Descriptor Heap";

            return true;
        }
        /// <inheritdoc/>
        public void Shutdown()
        {
            gfxCommand.Release();

            // NOTE: we don't call process_deferred_releases at the end because
            //       some resources (such as swap chains) can't be released before
            //       their depending resources are released.
            for (int i = 0; i < FrameBufferCount; i++)
            {
                ProcessDeferredReleases(i);
            }

            // shutdown modules
            D3D12Shaders.Shutdown();

            dxgiFactory.Release();

            // NOTE: some modules free their descriptors when they shutdown.
            //       We process those by calling process_deferred_free once more.
            rtvDescHeap.ProcessDeferredFree(0);
            dsvDescHeap.ProcessDeferredFree(0);
            srvDescHeap.ProcessDeferredFree(0);
            uavDescHeap.ProcessDeferredFree(0);

            // NOTE: some types only use deferred release for their resources during
            //       shutdown/reset/clear. To finally release these resources we call
            //       process_deferred_releases once more.
            ProcessDeferredReleases(0);

#if DEBUG
            {
                infoQueue.Dispose();

                var debugDevice = mainDevice.QueryInterface<ID3D12DebugDevice2>();
                mainDevice.Release();
                debugDevice.ReportLiveDeviceObjects(
                    ReportLiveDeviceObjectFlags.Summary |
                    ReportLiveDeviceObjectFlags.Detail |
                    ReportLiveDeviceObjectFlags.IgnoreInternal);
                debugDevice.Dispose();
            }
#endif

            mainDevice.Release();
        }
        /// <inheritdoc/>
        public string GetEngineShaderPath()
        {
            return EngineShaderPaths;
        }

        private IDXGIAdapter4 DetermineMainAdapter()
        {
            for (int i = 0; dxgiFactory.EnumAdapterByGpuPreference(i, GpuPreference.HighPerformance, out IDXGIAdapter4 adapter).Success; i++)
            {
                if (D3D12.D3D12CreateDevice<D3D12Device>(adapter, MinimumFeatureLevel, out _).Success)
                {
                    return adapter;
                }

                adapter.Release();
            }

            return null;
        }
        private bool FailedInit()
        {
            Shutdown();
            return false;
        }
        private static FeatureLevel GetMaxFeatureLevel(IDXGIAdapter4 adapter)
        {
            var featureLevels = new[]
            {
                FeatureLevel.Level_11_0,
                FeatureLevel.Level_11_1,
                FeatureLevel.Level_12_0,
                FeatureLevel.Level_12_1
            };

            using var device = D3D12.D3D12CreateDevice<D3D12Device>(adapter, MinimumFeatureLevel);

            return device.CheckMaxSupportedFeatureLevel(featureLevels);
        }
        private void ProcessDeferredReleases(int frameIdx)
        {
            lock (deferredReleasesMutex)
            {
                // NOTE: we clear this flag in the beginning. If we'd clear it at the end
                //       then it might overwrite some other thread that was trying to set it.
                //       It's fine if overwriting happens before processing the items.
                deferredReleasesFlags[frameIdx] = 0;

                rtvDescHeap.ProcessDeferredFree(frameIdx);
                dsvDescHeap.ProcessDeferredFree(frameIdx);
                srvDescHeap.ProcessDeferredFree(frameIdx);
                uavDescHeap.ProcessDeferredFree(frameIdx);

                var resources = deferredReleases[frameIdx];
                if (resources.Count <= 0)
                {
                    return;
                }

                foreach (var resource in resources)
                {
                    resource?.Dispose();
                }
                resources.Clear();
            }
        }
        public void DeferredRelease(IUnknown resource)
        {
            if (resource == null)
            {
                return;
            }

            int frameIdx = CurrentFrameIndex;
            lock (deferredReleasesMutex)
            {
                deferredReleases[frameIdx].Add(resource);
                SetDeferredReleasesFlag();
            }
        }

        /// <inheritdoc/>
        public ISurface CreateSurface(PlatformWindow window)
        {
            var surface = new D3D12Surface(window, this);
            surface.CreateSwapChain(dxgiFactory, gfxCommand.CommandQueue);

            surfaces.Add(surface);
            surface.Id = (uint)surfaces.Count - 1;

            return surface;
        }
        /// <inheritdoc/>
        public void RemoveSurface(uint id)
        {
            gfxCommand.Flush();
            surfaces[(int)id] = null;
        }
        /// <inheritdoc/>
        public void ResizeSurface(uint id, int width, int height)
        {
            gfxCommand.Flush();
            surfaces[(int)id].Resize(width, height);
        }
        /// <inheritdoc/>
        public int GetSurfaceWidth(uint id)
        {
            return surfaces[(int)id].Width;
        }
        /// <inheritdoc/>
        public int GetSurfaceHeight(uint id)
        {
            return surfaces[(int)id].Height;
        }
        /// <inheritdoc/>
        public void RenderSurface(uint id)
        {
            // Wait for the GPU to finish with the command allocator and
            // reset the allocator once the GPU is done with it.
            // This frees the memory that was used to store commands.
            gfxCommand.BeginFrame();
            var cmdList = gfxCommand.CommandList;

            int frameIdx = CurrentFrameIndex;
            if (deferredReleasesFlags[frameIdx] != 0)
            {
                ProcessDeferredReleases(frameIdx);
            }

            var surface = surfaces[(int)id];

            // Presenting swap chain buffers happens in lockstep with frame buffers.
            surface.Present();
            // Record commands
            // ...
            // 
            // Done recording commands. Now execute commands,
            // signal and increment the fence value for next frame.
            gfxCommand.EndFrame();
        }

#if DEBUG
        private static void DebugCallback(MessageCategory category, MessageSeverity severity, MessageId id, string description)
        {
            Debug.WriteLine($"{category} {severity} {id} {description}");
        }
#endif
    }
}

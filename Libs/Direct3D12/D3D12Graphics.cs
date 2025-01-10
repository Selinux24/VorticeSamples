using PrimalLike.Graphics;
using PrimalLike.Platform;
using SharpGen.Runtime;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Utilities;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.DXGI;

[assembly: InternalsVisibleTo("D3D12LibTests")]
namespace Direct3D12
{
#if DEBUG
    using Vortice.Direct3D12.Debug;
    using Vortice.DXGI.Debug;
#endif

    /// <summary>
    /// D3D12 graphics implementation.
    /// </summary>
    static class D3D12Graphics
    {
        /// <summary>
        /// Gets or sets the number of frame buffers.
        /// </summary>
        public const int FrameBufferCount = 3;
        private const FeatureLevel MinimumFeatureLevel = FeatureLevel.Level_11_0;
        private const string EngineShaderPaths = "Content/engineShaders.bin";

        private static DXGIAdapter mainAdapter;
        private static D3D12Device mainDevice;
#if DEBUG
        private static ID3D12InfoQueue infoQueue;
        private static readonly bool enableGPUBasedValidation = false;
#endif
        private static DXGIFactory dxgiFactory;
        private static D3D12Command gfxCommand;
        private static readonly FreeList<D3D12Surface> surfaces = new();
        private static readonly D3D12ResourceBarrier resourceBarriers = new();

        private static readonly D3D12DescriptorHeap rtvDescHeap = new(DescriptorHeapType.RenderTargetView);
        private static readonly D3D12DescriptorHeap dsvDescHeap = new(DescriptorHeapType.DepthStencilView);
        private static readonly D3D12DescriptorHeap srvDescHeap = new(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
        private static readonly D3D12DescriptorHeap uavDescHeap = new(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);

        private static readonly List<IUnknown>[] deferredReleases = new List<IUnknown>[FrameBufferCount];
        private static readonly bool[] deferredReleasesFlags = new bool[FrameBufferCount];
        private static readonly object deferredReleasesMutex = new();

        private static D3D12FrameInfo frameInfo = new();

        /// <summary>
        /// Gets the main D3D12 device.
        /// </summary>
        public static D3D12Device Device { get => mainDevice; }
        /// <summary>
        /// Gets the RTV descriptor heap.
        /// </summary>
        internal static D3D12DescriptorHeap RtvHeap { get => rtvDescHeap; }
        /// <summary>
        /// Gets the DSV descriptor heap.
        /// </summary>
        internal static D3D12DescriptorHeap DsvHeap { get => dsvDescHeap; }
        /// <summary>
        /// Gets the SRV descriptor heap.
        /// </summary>
        internal static D3D12DescriptorHeap SrvHeap { get => srvDescHeap; }
        /// <summary>
        /// Gets the UAV descriptor heap.
        /// </summary>
        internal static D3D12DescriptorHeap UavHeap { get => uavDescHeap; }
        /// <summary>
        /// Gets the current frame index.
        /// </summary>
        public static int CurrentFrameIndex { get => gfxCommand.FrameIndex; }

        /// <summary>
        /// Sets the deferred releases flag.
        /// </summary>
        public static void SetDeferredReleasesFlag()
        {
            deferredReleasesFlags[CurrentFrameIndex] = true;
        }

        /// <inheritdoc/>
        public static bool Initialize()
        {
            if (mainDevice != null)
            {
                Shutdown();
            }

            for (int i = 0; i < FrameBufferCount; i++)
            {
                deferredReleases[i] = [];
            }

#if DEBUG
            bool debug = true;
            InitializeDebugLayer();
#else
            bool debug = false;
#endif

            if (!D3D12Helpers.DxCall(DXGI.CreateDXGIFactory2(debug, out dxgiFactory)))
            {
                return FailedInit();
            }

            mainAdapter = DetermineMainAdapter();
            if (mainAdapter == null)
            {
                return FailedInit();
            }
            D3D12Helpers.NameDXGIObject(mainAdapter, "Main DXGI Adapter");

            var maxFeatureLevel = GetMaxFeatureLevel(mainAdapter);
            Debug.Assert(maxFeatureLevel >= MinimumFeatureLevel);
            if (maxFeatureLevel < MinimumFeatureLevel)
            {
                return FailedInit();
            }

            if (!D3D12Helpers.DxCall(D3D12.D3D12CreateDevice(mainAdapter, maxFeatureLevel, out mainDevice)))
            {
                return FailedInit();
            }
            D3D12Helpers.NameD3D12Object(mainDevice, "Main D3D12 Device");

#if DEBUG
            ConfigureInfoQueue();
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
            D3D12Helpers.NameD3D12Object(rtvDescHeap.Heap, "RTV Descriptor Heap");
            D3D12Helpers.NameD3D12Object(dsvDescHeap.Heap, "DSV Descriptor Heap");
            D3D12Helpers.NameD3D12Object(srvDescHeap.Heap, "SRV Descriptor Heap");
            D3D12Helpers.NameD3D12Object(uavDescHeap.Heap, "UAV Descriptor Heap");

            gfxCommand = new(CommandListType.Direct);
            if (gfxCommand.CommandQueue == null)
            {
                return FailedInit();
            }

            // initialize modules
            if (!D3D12Shaders.Initialize() ||
                !D3D12GPass.Initialize() ||
                !D3D12PostProcess.Initialize() ||
                !D3D12Upload.Initialize())
            {
                return FailedInit();
            }

            return true;
        }
        /// <inheritdoc/>
        public static void Shutdown()
        {
            // NOTE: we don't call process_deferred_releases at the end because
            //       some resources (such as swap chains) can't be released before
            //       their depending resources are released.
            for (int i = 0; i < FrameBufferCount; i++)
            {
                ProcessDeferredReleases(i);
            }

            // shutdown modules
            D3D12Upload.Shutdown();
            D3D12PostProcess.Shutdown();
            D3D12Shaders.Shutdown();
            D3D12GPass.Shutdown();

            dxgiFactory?.Dispose();
            dxgiFactory = null;

            // NOTE: some modules free their descriptors when they shutdown.
            //       We process those by calling process_deferred_free once more.
            rtvDescHeap?.ProcessDeferredFree(0);
            dsvDescHeap?.ProcessDeferredFree(0);
            srvDescHeap?.ProcessDeferredFree(0);
            uavDescHeap?.ProcessDeferredFree(0);

            rtvDescHeap?.Dispose();
            dsvDescHeap?.Dispose();
            srvDescHeap?.Dispose();
            uavDescHeap?.Dispose();

            // NOTE: some types only use deferred release for their resources during
            //       shutdown/reset/clear. To finally release these resources we call
            //       process_deferred_releases once more.
            ProcessDeferredReleases(0);

            gfxCommand?.Dispose();
            gfxCommand = null;

            resourceBarriers?.Dispose();

            mainAdapter?.Dispose();
            mainAdapter = null;

#if DEBUG
            ClearInfoQueueConfiguration();

            infoQueue?.Dispose();
            infoQueue = null;

            ID3D12DebugDevice2 debugDevice = null;
            var refCount = mainDevice.Release();
            if (refCount > 1)
            {
                debugDevice = mainDevice.QueryInterfaceOrNull<ID3D12DebugDevice2>();
            }
            mainDevice?.Dispose();
            mainDevice = null;

            if (debugDevice != null)
            {
                debugDevice.ReportLiveDeviceObjects(
                    ReportLiveDeviceObjectFlags.Detail |
                    ReportLiveDeviceObjectFlags.IgnoreInternal);
                debugDevice.Dispose();
            }

            if (DXGI.DXGIGetDebugInterface1(out IDXGIDebug1 dxgiDebug).Success)
            {
                dxgiDebug.ReportLiveObjects(DXGI.DebugAll, ReportLiveObjectFlags.Summary | ReportLiveObjectFlags.IgnoreInternal);
                dxgiDebug.Dispose();
            }
#else
            mainDevice?.Dispose();
            mainDevice = null;
#endif
        }
        /// <inheritdoc/>
        public static string GetEngineShaderPath()
        {
            return EngineShaderPaths;
        }

        private static IDXGIAdapter4 DetermineMainAdapter()
        {
            for (uint i = 0; D3D12Helpers.DxCall(dxgiFactory.EnumAdapterByGpuPreference(i, GpuPreference.HighPerformance, out IDXGIAdapter4 adapter)); i++)
            {
                if (D3D12Helpers.DxCall(D3D12.D3D12CreateDevice<D3D12Device>(adapter, MinimumFeatureLevel, out var tmpDevice)))
                {
                    tmpDevice.Dispose();
                    return adapter;
                }

                adapter.Dispose();
            }

            return null;
        }
        private static bool FailedInit()
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
        private static void ProcessDeferredReleases(int frameIdx)
        {
            lock (deferredReleasesMutex)
            {
                // NOTE: we clear this flag in the beginning. If we'd clear it at the end
                //       then it might overwrite some other thread that was trying to set it.
                //       It's fine if overwriting happens before processing the items.
                deferredReleasesFlags[frameIdx] = false;

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
        public static void DeferredRelease(IUnknown resource)
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
        public static ISurface CreateSurface(PlatformWindow window)
        {
            var surface = new D3D12Surface(window);
            surface.CreateSwapChain(dxgiFactory, gfxCommand.CommandQueue);
            surface.Id = (uint)surfaces.Add(surface);

            return surface;
        }
        /// <inheritdoc/>
        public static void RemoveSurface(uint id)
        {
            gfxCommand.Flush();
            surfaces[(int)id].Dispose();
            surfaces.Remove((int)id);
        }
        /// <inheritdoc/>
        public static void ResizeSurface(uint id, int width, int height)
        {
            gfxCommand.Flush();
            surfaces[(int)id].Resize(width, height);
        }
        /// <inheritdoc/>
        public static int GetSurfaceWidth(uint id)
        {
            return surfaces[(int)id].Width;
        }
        /// <inheritdoc/>
        public static int GetSurfaceHeight(uint id)
        {
            return surfaces[(int)id].Height;
        }
        /// <inheritdoc/>
        public static void RenderSurface(uint id)
        {
            // Wait for the GPU to finish with the command allocator and
            // reset the allocator once the GPU is done with it.
            // This frees the memory that was used to store commands.
            var cmdList = gfxCommand.BeginFrame();

            int frameIdx = CurrentFrameIndex;
            if (deferredReleasesFlags[frameIdx])
            {
                ProcessDeferredReleases(frameIdx);
            }

            var surface = surfaces[(int)id];

            frameInfo.SurfaceHeight = surface.Height;
            frameInfo.SurfaceWidth = surface.Width;

            D3D12GPass.SetSize(new(frameInfo.SurfaceWidth, frameInfo.SurfaceHeight));

            // Record commands
            cmdList.SetDescriptorHeaps([srvDescHeap.Heap]);
            cmdList.RSSetViewport(surface.Viewport);
            cmdList.RSSetScissorRect(surface.ScissorRect);

            var currentBackBuffer = surface.Backbuffer;

            // Depth prepass
            resourceBarriers.AddTransitionBarrier(
                currentBackBuffer,
                ResourceStates.Present,
                ResourceStates.RenderTarget,
                ResourceBarrierFlags.BeginOnly);
            D3D12GPass.AddTransitionsForDepthPrePass(resourceBarriers);
            resourceBarriers.Apply(cmdList);
            D3D12GPass.SetRenderTargetsForDepthPrePass(cmdList);
            D3D12GPass.DepthPrePass(cmdList, frameInfo);

            // Geometry and lighting pass
            D3D12GPass.AddTransitionsForGPass(resourceBarriers);
            resourceBarriers.Apply(cmdList);
            D3D12GPass.SetRenderTargetsForGPass(cmdList);
            D3D12GPass.Render(cmdList, frameInfo);

            // Post-process
            resourceBarriers.AddTransitionBarrier(
                currentBackBuffer,
                ResourceStates.Present,
                ResourceStates.RenderTarget,
                ResourceBarrierFlags.EndOnly);
            D3D12GPass.AddTransitionsForPostProcess(resourceBarriers);
            resourceBarriers.Apply(cmdList);

            // Will write to the current back buffer, so back buffer is a render target
            D3D12PostProcess.PostProcess(cmdList, surface.Rtv);

            // after post process
            cmdList.ResourceBarrierTransition(
                currentBackBuffer,
                ResourceStates.RenderTarget,
                ResourceStates.Present);

            // Done recording commands. Now execute commands,
            // signal and increment the fence value for next frame.
            gfxCommand.EndFrame(surface);

#if DEBUG
            PrintDebugMessage();
#endif
        }

#if DEBUG

        private static void InitializeDebugLayer()
        {
            if (D3D12Helpers.DxCall(D3D12.D3D12GetDebugInterface<ID3D12Debug3>(out var debugInterface)))
            {
                debugInterface.EnableDebugLayer();
                debugInterface.SetEnableGPUBasedValidation(enableGPUBasedValidation);
                debugInterface.Dispose();
            }
            else
            {
                Debug.WriteLine("Warning: D3D12 Debug interface is not available. Verify that Graphics Tools optional feature is installed on this system.");
            }

            if (D3D12Helpers.DxCall(D3D12.D3D12GetDebugInterface(out ID3D12DeviceRemovedExtendedDataSettings1 dredSettings)))
            {
                // Turn on auto-breadcrumbs and page fault reporting.
                dredSettings.SetAutoBreadcrumbsEnablement(DredEnablement.ForcedOn);
                dredSettings.SetPageFaultEnablement(DredEnablement.ForcedOn);
                dredSettings.SetBreadcrumbContextEnablement(DredEnablement.ForcedOn);
                dredSettings.Dispose();
            }
        }

        private static void ConfigureInfoQueue()
        {
            infoQueue =
                mainDevice.QueryInterfaceOrNull<ID3D12InfoQueue1>() ??
                mainDevice.QueryInterfaceOrNull<ID3D12InfoQueue>();

            if (infoQueue is ID3D12InfoQueue1 iq1)
            {
                iq1.RegisterMessageCallback(DebugCallback);
            }

            infoQueue.SetBreakOnSeverity(MessageSeverity.Corruption, true);
            infoQueue.SetBreakOnSeverity(MessageSeverity.Warning, true);
            infoQueue.SetBreakOnSeverity(MessageSeverity.Error, true);
        }
        private static void DebugCallback(MessageCategory category, MessageSeverity severity, MessageId id, string description)
        {
            Debug.WriteLine($"{category} {severity} {id} {description}");
        }
        private static void ClearInfoQueueConfiguration()
        {
            if (infoQueue is ID3D12InfoQueue1 iq1)
            {
                iq1.RegisterMessageCallback(null);
            }
            else
            {
                infoQueue = mainDevice.QueryInterfaceOrNull<ID3D12InfoQueue>();
            }

            infoQueue.SetBreakOnSeverity(MessageSeverity.Corruption, false);
            infoQueue.SetBreakOnSeverity(MessageSeverity.Warning, false);
            infoQueue.SetBreakOnSeverity(MessageSeverity.Error, false);
        }
        private static void PrintDebugMessage()
        {
            if (infoQueue == null)
            {
                return;
            }

            ulong numMessages = infoQueue.NumStoredMessages;
            if (numMessages == 0)
            {
                return;
            }

            for (ulong i = 0; i < numMessages; i++)
            {
                var messageInfo = infoQueue.GetMessage(i);

                Debug.WriteLine(messageInfo.Description.Replace("\0", string.Empty));
            }

            infoQueue.ClearStoredMessages();
        }

#endif
    }
}

using PrimalLike.Graphics;
using PrimalLike.Platform;
using SharpGen.Runtime;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.DXGI;
using System.Text;



#if DEBUG
using Vortice.Direct3D12.Debug;
#endif

namespace Direct3D12
{
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

        private static D3D12Device mainDevice;
#if DEBUG
        private static ID3D12InfoQueue infoQueue;
#endif
        private static IDXGIFactory7 dxgiFactory;
        private static D3D12Command gfxCommand;
        private static readonly List<D3D12Surface> surfaces = [];
        private static readonly D3D12ResourceBarrier resourceBarriers = new();

        private static readonly D3D12DescriptorHeap rtvDescHeap = new(DescriptorHeapType.RenderTargetView);
        private static readonly D3D12DescriptorHeap dsvDescHeap = new(DescriptorHeapType.DepthStencilView);
        private static readonly D3D12DescriptorHeap srvDescHeap = new(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
        private static readonly D3D12DescriptorHeap uavDescHeap = new(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);

        private static readonly List<IUnknown>[] deferredReleases = new List<IUnknown>[FrameBufferCount];
        private static readonly int[] deferredReleasesFlags = new int[FrameBufferCount];
        private static readonly Mutex deferredReleasesMutex = new();

        /// <summary>
        /// Gets the main D3D12 device.
        /// </summary>
        public static D3D12Device Device { get => mainDevice; }
        /// <summary>
        /// Gets the RTV descriptor heap.
        /// </summary>
        public static D3D12DescriptorHeap RtvHeap { get => rtvDescHeap; }
        /// <summary>
        /// Gets the DSV descriptor heap.
        /// </summary>
        public static D3D12DescriptorHeap DsvHeap { get => dsvDescHeap; }
        /// <summary>
        /// Gets the SRV descriptor heap.
        /// </summary>
        public static D3D12DescriptorHeap SrvHeap { get => srvDescHeap; }
        /// <summary>
        /// Gets the UAV descriptor heap.
        /// </summary>
        public static D3D12DescriptorHeap UavHeap { get => uavDescHeap; }
        /// <summary>
        /// Gets the current frame index.
        /// </summary>
        public static int CurrentFrameIndex { get => gfxCommand.FrameIndex; }

        /// <summary>
        /// Sets the deferred releases flag.
        /// </summary>
        public static void SetDeferredReleasesFlag()
        {
            deferredReleasesFlags[CurrentFrameIndex] = 1;
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

            bool debug = false;
#if DEBUG
            {
                if (D3D12Helpers.DxCall(D3D12.D3D12GetDebugInterface<ID3D12Debug>(out var debugInterface)))
                {
                    debugInterface.EnableDebugLayer();
                    debugInterface.Dispose();
                }
                else
                {
                    Debug.WriteLine("Warning: D3D12 Debug interface is not available. Verify that Graphics Tools optional feature is installed on this system.");
                }
                debug = true;

                if (D3D12Helpers.DxCall(D3D12.D3D12GetDebugInterface(out ID3D12DeviceRemovedExtendedDataSettings1 dredSettings)))
                {
                    // Turn on auto-breadcrumbs and page fault reporting.
                    dredSettings.SetAutoBreadcrumbsEnablement(DredEnablement.ForcedOn);
                    dredSettings.SetPageFaultEnablement(DredEnablement.ForcedOn);
                    dredSettings.SetBreadcrumbContextEnablement(DredEnablement.ForcedOn);
                    dredSettings.Dispose();
                }
            }
#endif
            if (!D3D12Helpers.DxCall(DXGI.CreateDXGIFactory2(debug, out dxgiFactory)))
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

            if (!D3D12Helpers.DxCall(D3D12.D3D12CreateDevice(mainAdapter, maxFeatureLevel, out mainDevice)))
            {
                return FailedInit();
            }
            mainDevice.Name = "Main D3D12 Device";

            gfxCommand = new D3D12Command(CommandListType.Direct);
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

            gfxCommand = new(CommandListType.Direct);
            if (gfxCommand.CommandQueue == null)
            {
                return FailedInit();
            }

            // initialize modules
            if (!D3D12Shaders.Initialize() || !D3D12GPass.Initialize() || !D3D12PostProcess.Initialize())
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
        public static void Shutdown()
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
            D3D12PostProcess.Shutdown();
            D3D12Shaders.Shutdown();
            D3D12GPass.Shutdown();

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
        public static string GetEngineShaderPath()
        {
            return EngineShaderPaths;
        }

        private static IDXGIAdapter4 DetermineMainAdapter()
        {
            for (int i = 0; D3D12Helpers.DxCall(dxgiFactory.EnumAdapterByGpuPreference(i, GpuPreference.HighPerformance, out IDXGIAdapter4 adapter)); i++)
            {
                if (D3D12Helpers.DxCall(D3D12.D3D12CreateDevice<D3D12Device>(adapter, MinimumFeatureLevel, out _)))
                {
                    return adapter;
                }

                adapter.Release();
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

            surfaces.Add(surface);
            surface.Id = (uint)surfaces.Count - 1;

            return surface;
        }
        /// <inheritdoc/>
        public static void RemoveSurface(uint id)
        {
            gfxCommand.Flush();
            surfaces[(int)id] = null;
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
            gfxCommand.BeginFrame();
            var cmdList = gfxCommand.CommandList;

            int frameIdx = CurrentFrameIndex;
            if (deferredReleasesFlags[frameIdx] != 0)
            {
                ProcessDeferredReleases(frameIdx);
            }

            var surface = surfaces[(int)id];
            var currentBackBuffer = surface.Backbuffer;

            D3D12FrameInfo frameInfo = new()
            {
                SurfaceHeight = surface.Height,
                SurfaceWidth = surface.Width,
            };

            D3D12GPass.SetSize(new(frameInfo.SurfaceWidth, frameInfo.SurfaceHeight));
            var barriers = resourceBarriers;

            // Record commands
            cmdList.SetDescriptorHeaps([srvDescHeap.Heap]);
            cmdList.RSSetViewport(surface.Viewport);
            cmdList.RSSetScissorRect(surface.ScissorRect);

            // Depth prepass
            barriers.Add(currentBackBuffer, ResourceStates.Present, ResourceStates.RenderTarget, ResourceBarrierFlags.BeginOnly);
            D3D12GPass.AddTransitionsForDepthPrePass(barriers);
            barriers.Apply(cmdList);
            D3D12GPass.SetRenderTargetsForDepthPrePass(cmdList);
            D3D12GPass.DepthPrePass(cmdList, frameInfo);

            // Geometry and lighting pass
            D3D12GPass.AddTransitionsForGPass(barriers);
            barriers.Apply(cmdList);
            D3D12GPass.SetRenderTargetsForGPass(cmdList);
            D3D12GPass.Render(cmdList, frameInfo);

            // Post-process
            barriers.Add(currentBackBuffer, ResourceStates.Present, ResourceStates.RenderTarget, ResourceBarrierFlags.EndOnly);
            D3D12GPass.AddTransitionsForPostProcess(barriers);
            barriers.Apply(cmdList);

            // Will write to the current back buffer, so back buffer is a render target
            D3D12PostProcess.PostProcess(cmdList, surface.Rtv);

            // after post process
            D3D12Helpers.TransitionResource(cmdList, currentBackBuffer, ResourceStates.RenderTarget, ResourceStates.Present);

            // Done recording commands. Now execute commands,
            // signal and increment the fence value for next frame.
            gfxCommand.EndFrame(surface);
        }

#if DEBUG
        private static void DebugCallback(MessageCategory category, MessageSeverity severity, MessageId id, string description)
        {
            Debug.WriteLine($"{category} {severity} {id} {description}");
        }

        public static string GetDebugMessage()
        {
            if (infoQueue == null)
            {
                return string.Empty;
            }

            ulong numMessages = infoQueue.NumStoredMessages;
            if(numMessages == 0)
            {
                return string.Empty;
            }

            StringBuilder message = new();

            for (ulong i = 0; i < numMessages; i++)
            {
                var messageInfo = infoQueue.GetMessage(i);

                string msgDescription = messageInfo.Description.Replace('\0', ' ');

                message.AppendLine(msgDescription);
            }

            infoQueue.ClearStoredMessages();

            return message.ToString();
        }
#endif
    }
}

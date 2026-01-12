using PrimalLike.EngineAPI;
using PrimalLike.Graphics;
using SharpGen.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Utilities;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.DXGI;

[assembly: InternalsVisibleTo("D3D12LibTests")]
namespace Direct3D12
{
    using Direct3D12.ShaderCompiler;
    using Direct3D12.Delight;
    using Direct3D12.Fx;
    using Direct3D12.Lights;
#if DEBUG
    using Vortice.Direct3D12.Debug;
    using Vortice.DXGI.Debug;
#endif

    /// <summary>
    /// D3D12 graphics implementation.
    /// </summary>
    static class D3D12Graphics
    {
        struct Option()
        {
            public bool EnableVsync = true;
            public bool EnableDxr = false;
            public uint MSAASamples = 1;
        }

        /// <summary>
        /// Gets or sets the number of frame buffers.
        /// </summary>
        public const int FrameBufferCount = 3;
        public const Format DefaultBackBufferFormat = Format.R16G16B16A16_Float;

        const uint D3d12SdkVersion = 618;
        const string D3d12SdkPath = "./D3D12/";
        const FeatureLevel MinimumFeatureLevel = FeatureLevel.Level_11_0;

        const string EngineShaderPaths = "Content/engineShaders.bin";
        const string EngineSourceShaderPaths = "../../../../../../Libs/Direct3D12/Hlsl/";
        const string EngineSourceShadersIncludeDir = "../../../../../../Libs/Direct3D12/Hlsl/";
        static readonly ShaderInfo[] engineShaderFiles =
        [
            new ((int)EngineShaders.FullScreenTriangleVs, new (Path.Combine(EngineSourceShaderPaths, "FullScreenTriangle.hlsl"), "FullScreenTriangleVS", (uint)ShaderStage.Vertex)),
            new ((int)EngineShaders.PostProcessPs, new (Path.Combine(EngineSourceShaderPaths, "PostProcess.hlsl"), "PostProcessPS", (uint)ShaderStage.Pixel)),
            new ((int)EngineShaders.GridFrustumsCs, new (Path.Combine(EngineSourceShaderPaths, "GridFrustums.hlsl"), "ComputeGridFrustumsCS", (uint)ShaderStage.Compute), ["-D", "TILE_SIZE=32"]),
            new ((int)EngineShaders.LightCullingCs, new (Path.Combine(EngineSourceShaderPaths, "CullLights.hlsl"), "CullLightsCS", (uint)ShaderStage.Compute), ["-D", "TILE_SIZE=32"]),
        ];

        static ID3D12SDKConfiguration1 d3d12SdkConfig;
        static ID3D12DeviceFactory d3d12DeviceFactory;
        static DXGIAdapter mainAdapter;
        static D3D12Device mainDevice;
#if DEBUG
        static ID3D12InfoQueue infoQueue;
        static readonly bool enableGPUBasedValidation = false;
#endif
        static DXGIFactory dxgiFactory;
        static D3D12Command gfxCommand;
        static readonly FreeList<D3D12Surface> surfaces = new();
        static readonly D3D12ResourceBarrier resourceBarriers = new();
        static readonly ConstantBuffer[] constantBuffers = new ConstantBuffer[FrameBufferCount];

        static readonly DescriptorHeap rtvDescHeap = new(DescriptorHeapType.RenderTargetView);
        static readonly DescriptorHeap dsvDescHeap = new(DescriptorHeapType.DepthStencilView);
        static readonly DescriptorHeap srvDescHeap = new(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
        static readonly DescriptorHeap uavDescHeap = new(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);

        static readonly List<IUnknown>[] deferredReleases = new List<IUnknown>[FrameBufferCount];
        static readonly int[] deferredReleasesFlags = new int[FrameBufferCount];
        static readonly Lock deferredReleasesMutex = new();

        static bool tearingIsSupported = false;
        static Option options = new();

        /// <summary>
        /// Gets the main D3D12 device.
        /// </summary>
        public static D3D12Device Device { get => mainDevice; }
        /// <summary>
        /// Gets the RTV descriptor heap.
        /// </summary>
        public static DescriptorHeap RtvHeap { get => rtvDescHeap; }
        /// <summary>
        /// Gets the DSV descriptor heap.
        /// </summary>
        public static DescriptorHeap DsvHeap { get => dsvDescHeap; }
        /// <summary>
        /// Gets the SRV descriptor heap.
        /// </summary>
        public static DescriptorHeap SrvHeap { get => srvDescHeap; }
        /// <summary>
        /// Gets the UAV descriptor heap.
        /// </summary>
        public static DescriptorHeap UavHeap { get => uavDescHeap; }
        /// <summary>
        /// Gets the constant buffer.
        /// </summary>
        public static ConstantBuffer CBuffer { get => constantBuffers[CurrentFrameIndex]; }
        /// <summary>
        /// Gets the current frame index.
        /// </summary>
        public static int CurrentFrameIndex { get => gfxCommand.FrameIndex; }

        public static bool IsWindows11OrGreater()
        {
            Debug.WriteLine(Environment.OSVersion);

            //Detect windows 11
            if (Environment.OSVersion.Version.Major >= 10 &&
                Environment.OSVersion.Version.Minor >= 0 &&
                Environment.OSVersion.Version.Build >= 22000)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Sets the deferred releases flag.
        /// </summary>
        public static void SetDeferredReleasesFlag(int frameIdx)
        {
            lock (deferredReleasesMutex)
            {
                deferredReleasesFlags[frameIdx] = 1;
            }
        }
        public static bool AllowTearing()
        {
            return tearingIsSupported;
        }
        public static bool VSyncEnabled()
        {
            return options.EnableVsync;
        }

        public static bool CompileShaders()
        {
            return Compiler.CompileShaders(engineShaderFiles, EngineSourceShadersIncludeDir, EngineShaderPaths);
        }
        public static string GetEngineShaderPath()
        {
            return EngineShaderPaths;
        }

        public static bool Initialize()
        {
            if (mainDevice != null)
            {
                Shutdown();
            }

            if (!D3D12Helpers.DxCall(D3D12.D3D12GetInterface(D3D12.D3D12SDKConfigurationClsId, out d3d12SdkConfig)))
            {
                return FailedInit();
            }

            if (!D3D12Helpers.DxCall(d3d12SdkConfig.CreateDeviceFactory(D3d12SdkVersion, D3d12SdkPath, out d3d12DeviceFactory)))
            {
                return FailedInit();
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

            tearingIsSupported = false;

#if true
            dxgiFactory.CheckFeatureSupport(Vortice.DXGI.Feature.PresentAllowTearing, tearingIsSupported);
#else
#pragma message("TEARING SUPPORT HAS BEEN DISABLED IN D3D12CORE::INITIALIZE()!")
#endif

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

            if (!D3D12Helpers.DxCall(d3d12DeviceFactory.CreateDevice(mainAdapter, maxFeatureLevel, out mainDevice)))
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

            for (uint i = 0; i < FrameBufferCount; i++)
            {
                constantBuffers[i] = new ConstantBuffer(ConstantBuffer.GetDefaultInitInfo(1024 * 1024));
                D3D12Helpers.NameD3D12Object(constantBuffers[i].Buffer, i, "Global Constant Buffer");
            }

            gfxCommand = new(CommandListType.Direct);
            if (gfxCommand.CommandQueue == null)
            {
                return FailedInit();
            }

            // initialize modules
            if (!D3D12Shaders.Initialize() ||
                !D3D12GPass.Initialize() ||
                !D3D12PostProcess.Initialize() ||
                !D3D12Upload.Initialize() ||
                !D3D12Content.Initialize() ||
                !D3D12LightCulling.Initialize())
            {
                return FailedInit();
            }

            return true;
        }
        public static void Shutdown()
        {
            // NOTE: we don't call process_deferred_releases at the end because
            //       some resources (such as swap chains) can't be released before
            //       their depending resources are released.
            for (int i = 0; i < FrameBufferCount; i++)
            {
                ProcessDeferredReleases(i, true);
            }

            // shutdown modules
            D3D12LightCulling.Shutdown();
            D3D12Content.Shutdown();
            D3D12Upload.Shutdown();
            D3D12PostProcess.Shutdown();
            D3D12Shaders.Shutdown();
            D3D12GPass.Shutdown();

            dxgiFactory?.Dispose();
            dxgiFactory = null;

            for (uint i = 0; i < FrameBufferCount; i++)
            {
                constantBuffers[i].Dispose();
            }

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
            ProcessDeferredReleases(0, true);

            gfxCommand?.Dispose();
            gfxCommand = null;

            resourceBarriers?.Dispose();

            mainAdapter?.Dispose();
            mainAdapter = null;

            d3d12DeviceFactory?.Dispose();
            d3d12DeviceFactory = null;
            d3d12SdkConfig?.Dispose();
            d3d12SdkConfig = null;

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
            if (!IsWindows11OrGreater())
            {
                mainDevice?.Dispose();
                mainDevice = null;
            }

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

        static IDXGIAdapter4 DetermineMainAdapter()
        {
            for (uint i = 0; D3D12Helpers.DxCall(dxgiFactory.EnumAdapterByGpuPreference(i, GpuPreference.HighPerformance, out IDXGIAdapter4 adapter)); i++)
            {
                if (D3D12Helpers.DxCall(d3d12DeviceFactory.CreateDevice<D3D12Device>(adapter, MinimumFeatureLevel, out var tmpDevice)))
                {
                    tmpDevice.Dispose();
                    return adapter;
                }

                adapter.Dispose();
            }

            return null;
        }
        static bool FailedInit()
        {
            Shutdown();
            return false;
        }
        static FeatureLevel GetMaxFeatureLevel(IDXGIAdapter4 adapter)
        {
            var featureLevels = new[]
            {
                FeatureLevel.Level_11_0,
                FeatureLevel.Level_11_1,
                FeatureLevel.Level_12_0,
                FeatureLevel.Level_12_1
            };

            using var device = d3d12DeviceFactory.CreateDevice<D3D12Device>(adapter, MinimumFeatureLevel);

            return device.CheckMaxSupportedFeatureLevel(featureLevels);
        }
        static void ProcessDeferredReleases(int frameIdx, bool forceRelease = false)
        {
            lock (deferredReleasesMutex)
            {
                // NOTE: The resources could still be in use in previous frames. So we wait
                //       another round before releasing the resources.
                //      force_release is used during shutdown, where we don't have to wait.
                if (!forceRelease && ++deferredReleasesFlags[frameIdx] < FrameBufferCount) return;

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
            if (resource == null) return;

            int frameIdx = CurrentFrameIndex;
            lock (deferredReleasesMutex)
            {
                deferredReleases[frameIdx].Add(resource);
                deferredReleasesFlags[frameIdx] = 1;
            }
        }

        static D3D12FrameInfo GetD3D12FrameInfo(FrameInfo info, D3D12Surface surface, uint frameIdx)
        {
            var camera = D3D12Camera.Get(info.CameraId);
            camera.Update();

            Shaders.GlobalShaderData data = new()
            {
                View = camera.View,
                Projection = camera.Projection,
                InvProjection = camera.InverseProjection,
                ViewProjection = camera.ViewProjection,
                InvViewProjection = camera.InverseViewProjection,
                CameraPosition = camera.Position,
                CameraDirection = camera.Direction,
                ViewWidth = surface.GetViewport().Width,
                ViewHeight = surface.GetViewport().Height,
                NumDirectionalLights = D3D12Light.NonCullableLightCount(info.LightSetKey),
                DeltaTime = info.AverageFrameTime,
                AmbientLight = D3D12Light.AmbientLight(info.LightSetKey),
            };

            ulong globalShaderDataAddress = CBuffer.Write(data);

            return new()
            {
                FrameInfo = info,
                Camera = camera,
                GlobalShaderData = globalShaderDataAddress,
                SurfaceWidth = surface.Width,
                SurfaceHeight = surface.Height,
                LightCullingId = surface.LightCullingId,
                FrameIndex = frameIdx,
            };
        }

        public static Surface CreateSurface(Window window)
        {
            uint id = surfaces.Add(new D3D12Surface(window));
            surfaces[id].CreateSwapChain(dxgiFactory, gfxCommand.CommandQueue);
            return new Surface(id);
        }
        public static void RemoveSurface(uint id)
        {
            gfxCommand.Flush();
            surfaces.Remove(id);
        }
        public static void ResizeSurface(uint id)
        {
            gfxCommand.Flush();
            surfaces[id].Resize();
        }
        public static uint GetSurfaceWidth(uint id)
        {
            return surfaces[id].Width;
        }
        public static uint GetSurfaceHeight(uint id)
        {
            return surfaces[id].Height;
        }
        public static void RenderSurface(uint id, FrameInfo info)
        {
            // Wait for the GPU to finish with the command allocator and
            // reset the allocator once the GPU is done with it.
            // This frees the memory that was used to store commands.
            var cmdList = gfxCommand.BeginFrame();

            int frameIdx = CurrentFrameIndex;

            // Reset (clear) the global constant buffer for the current frame.
            CBuffer.Clear();

            if (deferredReleasesFlags[frameIdx] != 0)
            {
                ProcessDeferredReleases(frameIdx);
            }

            var surface = surfaces[id];
            var d3d12Info = GetD3D12FrameInfo(info, surface, (uint)frameIdx);

            D3D12GPass.SetSize(d3d12Info.SurfaceWidth, d3d12Info.SurfaceHeight);

            // Record commands
            cmdList.SetDescriptorHeaps(srvDescHeap.Heap);
            cmdList.RSSetViewports(surface.GetViewport());
            cmdList.RSSetScissorRects(surface.GetScissorRect());

            var currentBackBuffer = surface.GetBackbuffer();

            // Depth prepass
            resourceBarriers.AddTransitionBarrier(
                currentBackBuffer,
                ResourceStates.Present,
                ResourceStates.RenderTarget,
                ResourceBarrierFlags.BeginOnly);
            D3D12GPass.AddTransitionsForDepthPrePass(resourceBarriers);
            resourceBarriers.Apply(cmdList);
            D3D12GPass.SetRenderTargetsForDepthPrePass(cmdList);
            D3D12GPass.DepthPrePass(cmdList, ref d3d12Info);

            // Geometry and lighting pass
            D3D12GPass.AddTransitionsForGPass(resourceBarriers);
            D3D12Light.UpdateLightBuffers(ref d3d12Info);
            D3D12LightCulling.CullLights(cmdList, ref d3d12Info, resourceBarriers);
            resourceBarriers.Apply(cmdList);
            D3D12GPass.SetRenderTargetsForGPass(cmdList);
            D3D12GPass.Render(cmdList, ref d3d12Info);

            // Post-process
            D3D12GPass.AddTransitionsForPostProcess(resourceBarriers);
            resourceBarriers.AddTransitionBarrier(
                currentBackBuffer,
                ResourceStates.Present,
                ResourceStates.RenderTarget,
                ResourceBarrierFlags.EndOnly);
            resourceBarriers.Apply(cmdList);

            // Will write to the current back buffer, so back buffer is a render target
            D3D12PostProcess.PostProcess(cmdList, ref d3d12Info, surface.GetRtv().Cpu);

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

        public static void SetOption<T>(RendererOption option, T parameter)
        {
            Debug.Assert(parameter != null);

            switch (option)
            {
                case RendererOption.VSync:
                    Debug.Assert(parameter is bool);
                    options.EnableVsync = (bool)(object)parameter || tearingIsSupported;
                    break;
                case RendererOption.RayTracing:
                    Debug.Assert(parameter is bool);
                    options.EnableDxr = (bool)(object)parameter;
                    break;
                case RendererOption.MSAA:
                    Debug.Assert(parameter is uint);
                    uint msaaSamples = (uint)(object)parameter;
                    if (msaaSamples == 1 || msaaSamples == 2 || msaaSamples == 4 || msaaSamples == 8)
                    {
                        options.MSAASamples = msaaSamples;
                    }
                    break;
                default:
                    break;
            }
        }
        public static T GetOption<T>(RendererOption option)
        {
            switch (option)
            {
                case RendererOption.VSync:
                    Debug.Assert(typeof(T) == typeof(bool));
                    return (T)(object)options.EnableVsync;
                case RendererOption.RayTracing:
                    Debug.Assert(typeof(T) == typeof(bool));
                    return (T)(object)options.EnableDxr;
                case RendererOption.MSAA:
                    Debug.Assert(typeof(T) == typeof(uint));
                    return (T)(object)options.MSAASamples;
                default:
                    return default;
            }
        }

#if DEBUG

        static readonly MessageSeverity[] SeverityBreakers =
        [
            MessageSeverity.Warning,
            MessageSeverity.Error,
            MessageSeverity.Corruption,
        ];

        static void InitializeDebugLayer()
        {
            if (D3D12Helpers.DxCall(d3d12DeviceFactory.GetConfigurationInterface<ID3D12Debug3>(D3D12.D3D12DebugClsId, out var debugInterface)))
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

        static void ConfigureInfoQueue()
        {
            infoQueue =
                mainDevice.QueryInterfaceOrNull<ID3D12InfoQueue1>() ??
                mainDevice.QueryInterfaceOrNull<ID3D12InfoQueue>();

            if (infoQueue is ID3D12InfoQueue1 iq1)
            {
                iq1.RegisterMessageCallback(DebugCallback);
            }

            foreach (var severity in SeverityBreakers)
            {
                infoQueue.SetBreakOnSeverity(severity, true);
            }
        }
        static void DebugCallback(MessageCategory category, MessageSeverity severity, MessageId id, string description)
        {
            Debug.WriteLine($"{category} {severity} {id} {description}");
        }
        static void ClearInfoQueueConfiguration()
        {
            if (infoQueue is ID3D12InfoQueue1 iq1)
            {
                iq1.RegisterMessageCallback(null);
            }
            else
            {
                infoQueue = mainDevice.QueryInterfaceOrNull<ID3D12InfoQueue>();
            }

            foreach (var severity in SeverityBreakers)
            {
                infoQueue.SetBreakOnSeverity(severity, false);
            }
        }
        static void PrintDebugMessage()
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

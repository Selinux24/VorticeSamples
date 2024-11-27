using Engine.Graphics;
using Engine.Platform;
using SharpGen.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.Direct3D12.Debug;
using Vortice.DXGI;

namespace Direct3D12
{
    class D3D12Graphics : GraphicsBase
    {
        private const FeatureLevel MinimumFeatureLevel = FeatureLevel.Level_11_0;
        public static int FrameBufferCount { get; set; } = 3;

        private ID3D12Device8 mainDevice;
        private IDXGIFactory7 dxgiFactory;
        private D3D12Command gfxCommand;

        private readonly DescriptorHeap rtvDescHeap;
        private readonly DescriptorHeap dsvDescHeap;
        private readonly DescriptorHeap srvDescHeap;
        private readonly DescriptorHeap uavDescHeap;

        private readonly List<IUnknown>[] deferredReleases;
        private readonly int[] deferredReleasesFlags;
        private readonly Mutex deferredReleasesMutx;

        public ID3D12Device Device { get => mainDevice; }

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
            deferredReleasesMutx = new();
        }

        public int CurrentFrameIndex() => gfxCommand.FrameIndex;
        public void SetDeferredReleasesFlag()
        {
            deferredReleasesFlags[CurrentFrameIndex()] = 1;
        }

        /// <inheritdoc/>
        public override bool Initialize()
        {
            if (mainDevice != null)
            {
                Shutdown();
            }

            bool dxgiFactoryFlags = false;
#if DEBUG
            {
                if (D3D12.D3D12GetDebugInterface<ID3D12Debug3>(out var debugInterface).Success)
                {
                    debugInterface.EnableDebugLayer();
                }
                else
                {
                    Console.WriteLine("Warning: D3D12 Debug interface is not available. Verify that Graphics Tools optional feature is installed on this system.");
                }
                dxgiFactoryFlags = true;
            }
#endif
            if (!DXGI.CreateDXGIFactory2(dxgiFactoryFlags, out dxgiFactory).Success)
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

            gfxCommand = new D3D12Command(mainDevice, CommandListType.Direct);
            if (gfxCommand.CommandQueue == null)
            {
                return FailedInit();
            }

#if DEBUG
            {
                var infoQueue = mainDevice.QueryInterface<ID3D12InfoQueue>();
                infoQueue.SetBreakOnSeverity(MessageSeverity.Corruption, true);
                infoQueue.SetBreakOnSeverity(MessageSeverity.Warning, true);
                infoQueue.SetBreakOnSeverity(MessageSeverity.Error, true);
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

            gfxCommand = new(mainDevice, CommandListType.Direct);
            if (gfxCommand.CommandQueue == null)
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
        public override void Shutdown()
        {
            gfxCommand.Release();

            // NOTE: we don't call process_deferred_releases at the end because
            //       some resources (such as swap chains) can't be released before
            //       their depending resources are released.
            for (int i = 0; i < FrameBufferCount; i++)
            {
                ProcessDeferredReleases(i);
            }

            dxgiFactory.Release();

            rtvDescHeap.Release();
            dsvDescHeap.Release();
            srvDescHeap.Release();
            uavDescHeap.Release();

            // NOTE: some types only use deferred release for their resources during
            //       shutdown/reset/clear. To finally release these resources we call
            //       process_deferred_releases once more.
            ProcessDeferredReleases(0);

#if DEBUG
            {
                var infoQueue = mainDevice.QueryInterface<ID3D12InfoQueue>();
                infoQueue.SetBreakOnSeverity(MessageSeverity.Corruption, false);
                infoQueue.SetBreakOnSeverity(MessageSeverity.Warning, false);
                infoQueue.SetBreakOnSeverity(MessageSeverity.Error, false);

                var debugDevice = mainDevice.QueryInterface<ID3D12DebugDevice2>();
                mainDevice.Release();
                debugDevice.ReportLiveDeviceObjects(
                    ReportLiveDeviceObjectFlags.Summary |
                    ReportLiveDeviceObjectFlags.Detail |
                    ReportLiveDeviceObjectFlags.IgnoreInternal);
            }
#endif

            mainDevice.Release();
        }
        /// <inheritdoc/>
        public override void Render()
        {
            // Wait for the GPU to finish with the command allocator and
            // reset the allocator once the GPU is done with it.
            // This frees the memory that was used to store commands.
            gfxCommand.BeginFrame();
            var cmdlist = gfxCommand.CommandList;

            int frameIdx = CurrentFrameIndex();
            if (deferredReleasesFlags[frameIdx] != 0)
            {
                ProcessDeferredReleases(frameIdx);
            }
            // Record commands
            // ...
            // 
            // Done recording commands. Now execute commands,
            // signal and increment the fence value for next frame.
            gfxCommand.EndFrame();
        }

        private IDXGIAdapter4 DetermineMainAdapter()
        {
            for (int i = 0; dxgiFactory.EnumAdapterByGpuPreference(i, GpuPreference.HighPerformance, out IDXGIAdapter4 adapter).Success; i++)
            {
                if (D3D12.D3D12CreateDevice<ID3D12Device8>(adapter, MinimumFeatureLevel, out _).Success)
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

            using var device = D3D12.D3D12CreateDevice<ID3D12Device8>(adapter, MinimumFeatureLevel);

            return device.CheckMaxSupportedFeatureLevel(featureLevels);
        }
        private void ProcessDeferredReleases(int frameIdx)
        {
            lock (deferredReleasesMutx)
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
                if (resources.Count > 0)
                {
                    foreach (var resource in resources)
                    {
                        resource?.Dispose();
                    }
                    resources.Clear();
                }
            }
        }
        public void DeferredRelease(IUnknown resource)
        {
            int frameIdx = CurrentFrameIndex();
            lock (deferredReleasesMutx)
            {
                deferredReleases[frameIdx].Add(resource);
                SetDeferredReleasesFlag();
            }
        }

        /// <inheritdoc/>
        public override ISurface CreateSurface(PlatformWindow window)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void RemoveSurface(uint id)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void ResizeSurface(uint id, uint width, uint height)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void RenderSurface(uint id, IFrameInfo info)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override ICamera CreateCamera(ICameraInitInfo info)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void RemoveCamera(uint id)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void SetCameraParameter<T>(uint id, ICameraParameters parameter, T value)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override T GetCameraParameter<T>(uint id, ICameraParameters parameter)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override void CreateLightSet(ulong lightSetKey)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void RemoveLightSet(ulong lightSetKey)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override ILight CreateLight(ILightInitInfo info)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void RemoveLight(uint id, ulong lightSetKey)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void SetLightParameter<T>(uint id, ulong lightSetKey, ILightParameters parameter, T value)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override T GetLightParameter<T>(uint id, ulong lightSetKey, ILightParameters parameter)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override uint AddSubmesh(byte[] data)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void RemoveSubmesh(uint id)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override uint AddTexture(byte[] data)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void RemoveTexture(uint id)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override uint AddMaterial(IMaterialInfo info)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void RemoveMaterial(uint id)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override uint AddRenderItem(uint entityId, uint geometryContentId, uint[] materialIds)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc/>
        public override void RemoveRenderItem(uint id)
        {
            throw new NotImplementedException();
        }
    }
}

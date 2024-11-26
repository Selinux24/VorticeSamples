using Engine.Graphics;
using Engine.Platform;
using System;
using System.Diagnostics;
using Vortice.Direct3D12.Debug;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Direct3D;

namespace Direct3D12
{
    public class D3D12Graphics : GraphicsBase
    {
        private const FeatureLevel MinimumFeatureLevel = FeatureLevel.Level_11_0;
      
        private ID3D12Device8 mainDevice;
        private IDXGIFactory7 dxgiFactory;

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
                var debugInterface = D3D12.D3D12GetDebugInterface<ID3D12Debug3>();
                debugInterface.EnableDebugLayer();
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

#if DEBUG
            {
                var infoQueue = mainDevice.QueryInterface<ID3D12InfoQueue>();
                infoQueue.SetBreakOnSeverity(MessageSeverity.Corruption, true);
                infoQueue.SetBreakOnSeverity(MessageSeverity.Warning, true);
                infoQueue.SetBreakOnSeverity(MessageSeverity.Error, true);
            }
#endif

            return true;
        }
        /// <inheritdoc/>
        public override void Shutdown()
        {
            dxgiFactory.Release();

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

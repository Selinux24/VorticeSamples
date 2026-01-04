using DirectXTexNet;
using System;
using System.Collections.Generic;
using System.Threading;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace TexturesImporter
{
    class DeviceManager() : IDisposable
    {
        struct D3D11Device()
        {
            public ID3D11Device Device = null;
            public object HwCompressionMutex = new();
        }

        private const uint WarpId = 0x1414;

        private readonly Lock deviceCreationMutex = new();
        private bool tryOnce = false;
        private readonly List<D3D11Device> d3d11Devices = [];

        ~DeviceManager()
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
                for (int i = 0; i < d3d11Devices.Count; i++)
                {
                    d3d11Devices[i].Device?.Dispose();
                }

                d3d11Devices.Clear();
            }
        }

        public static bool CanUseGpu(DXGI_FORMAT format)
        {
            return format switch
            {
                DXGI_FORMAT.BC6H_TYPELESS or
                DXGI_FORMAT.BC6H_UF16 or
                DXGI_FORMAT.BC6H_SF16 or
                DXGI_FORMAT.BC7_TYPELESS or
                DXGI_FORMAT.BC7_UNORM or
                DXGI_FORMAT.BC7_UNORM_SRGB => true,
                _ => false,
            };
        }
        public static bool IsHdr(DXGI_FORMAT format)
        {
            return format switch
            {
                DXGI_FORMAT.BC6H_UF16 or
                DXGI_FORMAT.BC6H_SF16 or
                DXGI_FORMAT.R9G9B9E5_SHAREDEXP or
                DXGI_FORMAT.R10G10B10A2_UINT or
                DXGI_FORMAT.R16G16B16A16_FLOAT or
                DXGI_FORMAT.R32G32B32A32_FLOAT or
                DXGI_FORMAT.R32G32B32_FLOAT => true,
                _ => false,
            };
        }

        private void CreateDevice()
        {
            if (d3d11Devices.Count > 0)
            {
                return;
            }

            var adapters = GetAdaptersByPerformance();

            DeviceCreationFlags createDeviceFlags = 0;
#if DEBUG
            createDeviceFlags |= DeviceCreationFlags.Debug;
#endif

            List<ID3D11Device> devices = [];
            FeatureLevel[] featureLevels = [FeatureLevel.Level_11_1];

            for (int i = 0; i < adapters.Length; i++)
            {
                if (D3D11.D3D11CreateDevice(
                    adapters[i],
                    adapters[i] != null ? DriverType.Unknown : DriverType.Hardware,
                    createDeviceFlags,
                    featureLevels, out var device).Success)
                {
                    devices.Add(device);
                }

                adapters[i].Dispose();
                adapters[i] = null;
            }

            for (int i = 0; i < devices.Count; i++)
            {
                // NOTE: we check for valid devices since device creation can fail for adapters that don't support
                //       the requested feature level (D3D_FEATURE_LEVEL_11_1).
                if (devices[i] != null)
                {
                    d3d11Devices.Add(new() { Device = devices[i] });
                }
            }
        }
        static IDXGIAdapter[] GetAdaptersByPerformance()
        {
            using var factory = DXGI.CreateDXGIFactory1<IDXGIFactory7>();

            List<IDXGIAdapter> adapters = [];

            for (uint i = 0; factory.EnumAdapterByGpuPreference(i, GpuPreference.HighPerformance, out IDXGIAdapter adapter).Success; i++)
            {
                if (adapter == null)
                {
                    continue;
                }

                var desc = adapter.Description;

                if (desc.VendorId != WarpId)
                {
                    adapters.Add(adapter);
                }
            }

            return [.. adapters];
        }

        public bool RunOnGPU(Func<ID3D11Device, bool> func)
        {
            if (!TryCreateDevice())
            {
                return false;
            }

            bool wait = true;
            bool result = false;
            while (wait)
            {
                for (int i = 0; i < d3d11Devices.Count; i++)
                {
                    if (Monitor.TryEnter(d3d11Devices[i].HwCompressionMutex))
                    {
                        result = func.Invoke(d3d11Devices[i].Device);
                        Monitor.Exit(d3d11Devices[i].HwCompressionMutex);
                        wait = false;
                        break;
                    }
                }
                if (wait)
                {
                    Thread.Sleep(200);
                }
            }

            return result;
        }
        bool TryCreateDevice()
        {
            lock (deviceCreationMutex)
            {
                tryOnce = false;
                if (!tryOnce)
                {
                    tryOnce = true;
                    CreateDevice();
                }
            }

            return d3d11Devices.Count > 0;
        }

        public ScratchImage CompressGpu(ScratchImage scratch, DXGI_FORMAT outputFormat)
        {
            ScratchImage bcScrath = null;

            bool wait = true;
            while (wait)
            {
                for (int i = 0; i < d3d11Devices.Count; i++)
                {
                    if (Monitor.TryEnter(d3d11Devices[i].HwCompressionMutex))
                    {
                        bcScrath = scratch.Compress(d3d11Devices[i].Device.NativePointer, outputFormat, TEX_COMPRESS_FLAGS.DEFAULT, 1.0f);
                        Monitor.Exit(d3d11Devices[i].HwCompressionMutex);
                        wait = false;
                        break;
                    }
                }
                if (wait)
                {
                    Thread.Sleep(200);
                }
            }

            return bcScrath;
        }
    }
}

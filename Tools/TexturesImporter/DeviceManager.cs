﻿using DirectXTexNet;
using System.Collections.Generic;
using System.Threading;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace TexturesImporter
{
    static class DeviceManager
    {
        struct D3D11Device()
        {
            public ID3D11Device Device = null;
            public object HwCompressionMutex = new();
        }

        private const uint WarpId = 0x1414;

        private static readonly object deviceCreationMutex = new();
        private static bool tryOnce = false;
        private static readonly List<D3D11Device> d3d11Devices = [];

        public static bool CanUseGpu(DXGI_FORMAT format)
        {
            switch (format)
            {
                case DXGI_FORMAT.BC6H_TYPELESS:
                case DXGI_FORMAT.BC6H_UF16:
                case DXGI_FORMAT.BC6H_SF16:
                case DXGI_FORMAT.BC7_TYPELESS:
                case DXGI_FORMAT.BC7_UNORM:
                case DXGI_FORMAT.BC7_UNORM_SRGB:
                {
                    lock (deviceCreationMutex)
                    {
                        if (!tryOnce)
                        {
                            tryOnce = true;
                            CreateDevice();
                        }

                        return d3d11Devices.Count > 0;
                    }
                }
            }

            return false;
        }
        private static IDXGIAdapter[] GetAdaptersByPerformance()
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
        private static void CreateDevice()
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
            FeatureLevel[] featureLevels = [FeatureLevel.Level_11_0];

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
                //       the requested feature level (D3D_FEATURE_LEVEL_11_0).
                if (devices[i] != null)
                {
                    d3d11Devices.Add(new() { Device = devices[i] });
                }
            }
        }

        public static ScratchImage CompressGpu(ScratchImage scratch, DXGI_FORMAT outputFormat)
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

        public static void ShutDown()
        {
            for (int i = 0; i < d3d11Devices.Count; i++)
            {
                d3d11Devices[i].Device?.Dispose();
            }

            d3d11Devices.Clear();
        }
    }
}

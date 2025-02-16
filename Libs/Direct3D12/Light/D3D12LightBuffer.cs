using Direct3D12.Helpers;
using Direct3D12.Shaders;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Direct3D12.Lights
{
    class D3D12LightBuffer : IDisposable
    {
        unsafe struct LightBuffer()
        {
            private D3D12Buffer buffer = new();
            private byte* cpuAddress = null;

            public readonly ulong GpuAddress => buffer.GpuAddress;
            public readonly uint Size => buffer.Size;

            public readonly void Write<T>(T[] lights) where T : unmanaged
            {
                Debug.Assert(cpuAddress != null);
                Debug.Assert(buffer.Size == D3D12Helpers.AlignSizeForConstantBuffer((ulong)(Marshal.SizeOf<T>() * lights.Length)));

                BuffersHelper.WriteArray(cpuAddress, lights);
            }
            public readonly void Write<T>(uint i, T light) where T : unmanaged
            {
                Debug.Assert(cpuAddress != null);
                Debug.Assert(buffer.Size >= Marshal.SizeOf<T>() * (i + 1));
                T* p = (T*)(cpuAddress + Marshal.SizeOf<T>() * i);

                BuffersHelper.Write(p, light);
            }

            public void Resize(LightBufferTypes type, uint size, uint frameIndex)
            {
                Debug.Assert(type < LightBufferTypes.Count);
                if (size == 0)
                {
                    return;
                }

                buffer.Dispose();
                buffer = new(ConstantBuffer.GetDefaultInitInfo(size), true);

                D3D12Helpers.NameD3D12Object(
                    buffer.Buffer,
                    frameIndex,
                    type == LightBufferTypes.NonCullableLight ? "Non-cullable Light Buffer" :
                    type == LightBufferTypes.CullableLight ? "Cullable Light Buffer" : "Light Culling Info Buffer");

                fixed (byte** cpuAddress = &this.cpuAddress)
                {
                    D3D12Helpers.DxCall(buffer.Buffer.Map(0, cpuAddress));
                    Debug.Assert(this.cpuAddress != null);
                }
            }
            public void Release()
            {
                buffer.Dispose();
                cpuAddress = null;
            }
        }

        enum LightBufferTypes : uint
        {
            NonCullableLight,
            CullableLight,
            CullingInfo,

            Count,
        }

        private readonly LightBuffer[] buffers;
        private ulong currentLightSetKey;

        public D3D12LightBuffer()
        {
            buffers = new LightBuffer[(uint)LightBufferTypes.Count];
            for (int i = 0; i < buffers.Length; i++)
            {
                buffers[i] = new();
            }

            currentLightSetKey = 0;
        }
        ~D3D12LightBuffer()
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
                for (uint i = 0; i < (uint)LightBufferTypes.Count; i++)
                {
                    buffers[i].Release();
                }
            }
        }

        public void UpdateLightBuffers(LightSet set, ulong lightSetKey, uint frameIndex)
        {
            uint[] sizes = new uint[(uint)LightBufferTypes.Count];
            sizes[(uint)LightBufferTypes.NonCullableLight] = set.NonCullableLightCount() * (uint)Marshal.SizeOf<DirectionalLightParameters>();
            sizes[(uint)LightBufferTypes.CullableLight] = set.CullableLightCount() * (uint)Marshal.SizeOf<LightParameters>();
            sizes[(uint)LightBufferTypes.CullingInfo] = set.CullableLightCount() * (uint)Marshal.SizeOf<LightCullingLightInfo>();

            uint[] currentSizes = new uint[(uint)LightBufferTypes.Count];
            currentSizes[(uint)LightBufferTypes.NonCullableLight] = buffers[(uint)LightBufferTypes.NonCullableLight].Size;
            currentSizes[(uint)LightBufferTypes.CullableLight] = buffers[(uint)LightBufferTypes.CullableLight].Size;
            currentSizes[(uint)LightBufferTypes.CullingInfo] = buffers[(uint)LightBufferTypes.CullingInfo].Size;

            if (currentSizes[(uint)LightBufferTypes.NonCullableLight] < sizes[(uint)LightBufferTypes.NonCullableLight])
            {
                buffers[(uint)LightBufferTypes.NonCullableLight].Resize(LightBufferTypes.NonCullableLight, sizes[(uint)LightBufferTypes.NonCullableLight], frameIndex);
            }

            set.NonCullableLights(out var lights);
            buffers[(uint)LightBufferTypes.NonCullableLight].Write(lights);

            // Update cullable light buffers
            bool buffersResized = false;
            if (currentSizes[(uint)LightBufferTypes.CullableLight] < sizes[(uint)LightBufferTypes.CullableLight])
            {
                Debug.Assert(currentSizes[(uint)LightBufferTypes.CullingInfo] < sizes[(uint)LightBufferTypes.CullingInfo]);
                buffers[(uint)LightBufferTypes.CullableLight].Resize(LightBufferTypes.CullableLight, sizes[(uint)LightBufferTypes.CullableLight], frameIndex);
                buffers[(uint)LightBufferTypes.CullingInfo].Resize(LightBufferTypes.CullingInfo, sizes[(uint)LightBufferTypes.CullingInfo], frameIndex);
                buffersResized = true;
            }

            bool allLightsUpdated = false;
            if (buffersResized || currentLightSetKey != lightSetKey)
            {
                set.CullableLights(out var cullableLights);
                set.CullingInfo(out var cullinginfo);
                buffers[(uint)LightBufferTypes.CullableLight].Write(cullableLights);
                buffers[(uint)LightBufferTypes.CullingInfo].Write(cullinginfo);

                currentLightSetKey = lightSetKey;
                allLightsUpdated = true;
            }

            Debug.Assert(currentLightSetKey == lightSetKey);
            uint indexMask = (uint)(1 << (int)frameIndex);

            if (allLightsUpdated)
            {
                for (uint i = 0; i < set.CullableLightCount(); i++)
                {
                    set.SetDirtyBit(i, indexMask);
                }
            }
            else
            {
                for (uint i = 0; i < set.CullableLightCount(); i++)
                {
                    if (!set.GetDirtyBit(i, indexMask))
                    {
                        continue;
                    }

                    Debug.Assert(i * Marshal.SizeOf<LightParameters>() < sizes[(uint)LightBufferTypes.CullableLight]);
                    Debug.Assert(i * Marshal.SizeOf<LightCullingLightInfo>() < sizes[(uint)LightBufferTypes.CullingInfo]);

                    var cullableLight = set.CullableLights(i);
                    var cullingInfo = set.CullingInfo(i);
                    buffers[(uint)LightBufferTypes.CullableLight].Write(i, cullableLight);
                    buffers[(uint)LightBufferTypes.CullingInfo].Write(i, cullingInfo);

                    set.SetDirtyBit(i, indexMask);
                }
            }
        }

        public ulong NonCullableLights()
        {
            return buffers[(uint)LightBufferTypes.NonCullableLight].GpuAddress;
        }
        public ulong CullableLights()
        {
            return buffers[(uint)LightBufferTypes.CullableLight].GpuAddress;
        }
        public ulong CullingInfo()
        {
            return buffers[(uint)LightBufferTypes.CullingInfo].GpuAddress;
        }
    }
}

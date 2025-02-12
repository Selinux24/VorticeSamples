using Direct3D12.Lights;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Direct3D12.Shaders
{
    class D3D12LightBuffer
    {
        unsafe struct LightBuffer()
        {
            public D3D12Buffer Buffer = new();
            public byte* CpuAddress = null;

            public readonly void Write<T>(T[] lights) where T : unmanaged
            {
                Debug.Assert(CpuAddress != null);
                Debug.Assert(Buffer.Size == D3D12Helpers.AlignSizeForConstantBuffer((ulong)(Marshal.SizeOf<T>() * lights.Length)));

                BuffersHelper.WriteArray(CpuAddress, lights);
            }
            public readonly void Write<T>(uint i, T light) where T : unmanaged
            {
                Debug.Assert(CpuAddress != null);
                Debug.Assert(Buffer.Size >= Marshal.SizeOf<T>() * (i + 1));
                T* p = (T*)(CpuAddress + Marshal.SizeOf<T>() * i);

                BuffersHelper.Write(p, light);
            }

            public void Resize()
            {
                fixed (byte** cpuAddress = &CpuAddress)
                {
                    D3D12Helpers.DxCall(Buffer.Buffer.Map(0, cpuAddress));
                    Debug.Assert(CpuAddress != null);
                }
            }
            public void Release()
            {
                Buffer.Release();
                CpuAddress = null;
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

        public void UpdateLightBuffers(LightSet set, ulong lightSetKey, uint frameIndex)
        {
            uint[] sizes = new uint[(uint)LightBufferTypes.Count];
            sizes[(uint)LightBufferTypes.NonCullableLight] = set.NonCullableLightCount() * (uint)Marshal.SizeOf<DirectionalLightParameters>();
            sizes[(uint)LightBufferTypes.CullableLight] = set.CullableLightCount() * (uint)Marshal.SizeOf<LightParameters>();
            sizes[(uint)LightBufferTypes.CullingInfo] = set.CullableLightCount() * (uint)Marshal.SizeOf<LightCullingLightInfo>();

            uint[] currentSizes = new uint[(uint)LightBufferTypes.Count];
            currentSizes[(uint)LightBufferTypes.NonCullableLight] = buffers[(uint)LightBufferTypes.NonCullableLight].Buffer.Size;
            currentSizes[(uint)LightBufferTypes.CullableLight] = buffers[(uint)LightBufferTypes.CullableLight].Buffer.Size;
            currentSizes[(uint)LightBufferTypes.CullingInfo] = buffers[(uint)LightBufferTypes.CullingInfo].Buffer.Size;

            if (currentSizes[(uint)LightBufferTypes.NonCullableLight] < sizes[(uint)LightBufferTypes.NonCullableLight])
            {
                ResizeBuffer(LightBufferTypes.NonCullableLight, sizes[(uint)LightBufferTypes.NonCullableLight], frameIndex);
            }

            set.NonCullableLights(out var lights);
            buffers[(uint)LightBufferTypes.NonCullableLight].Write(lights);

            // Update cullable light buffers
            bool buffersResized = false;
            if (currentSizes[(uint)LightBufferTypes.CullableLight] < sizes[(uint)LightBufferTypes.CullableLight])
            {
                Debug.Assert(currentSizes[(uint)LightBufferTypes.CullingInfo] < sizes[(uint)LightBufferTypes.CullingInfo]);
                ResizeBuffer(LightBufferTypes.CullableLight, sizes[(uint)LightBufferTypes.CullableLight], frameIndex);
                ResizeBuffer(LightBufferTypes.CullingInfo, sizes[(uint)LightBufferTypes.CullingInfo], frameIndex);
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
                    if (set.GetDirtyBit(i, indexMask))
                    {
                        Debug.Assert(i * Marshal.SizeOf<LightParameters>() < sizes[(uint)LightBufferTypes.CullableLight]);
                        Debug.Assert(i * (uint)Marshal.SizeOf<LightCullingLightInfo>() < sizes[(uint)LightBufferTypes.CullingInfo]);

                        var cullableLight = set.CullableLights(i);
                        var cullingInfo = set.CullingInfo(i);
                        buffers[(uint)LightBufferTypes.CullableLight].Write(i, cullableLight);
                        buffers[(uint)LightBufferTypes.CullingInfo].Write(i, cullingInfo);

                        set.SetDirtyBit(i, indexMask);
                    }
                }
            }
        }
        private void ResizeBuffer(LightBufferTypes type, uint size, uint frameIndex)
        {
            Debug.Assert(type < LightBufferTypes.Count);
            if (size == 0)
            {
                return;
            }

            buffers[(uint)type].Buffer.Release();
            buffers[(uint)type].Buffer = new(D3D12ConstantBuffer.GetDefaultInitInfo(size), true);

            D3D12Helpers.NameD3D12Object(
                buffers[(uint)type].Buffer.Buffer,
                frameIndex,
                type == LightBufferTypes.NonCullableLight ? "Non-cullable Light Buffer" :
                type == LightBufferTypes.CullableLight ? "Cullable Light Buffer" : "Light Culling Info Buffer");

            buffers[(uint)type].Resize();
        }

        public void Release()
        {
            for (uint i = 0; i < (uint)LightBufferTypes.Count; i++)
            {
                buffers[i].Release();
            }
        }

        public ulong NonCullableLights()
        {
            return buffers[(uint)LightBufferTypes.NonCullableLight].Buffer.GpuAddress;
        }
        public ulong CullableLights()
        {
            return buffers[(uint)LightBufferTypes.CullableLight].Buffer.GpuAddress;
        }
        public ulong CullingInfo()
        {
            return buffers[(uint)LightBufferTypes.CullingInfo].Buffer.GpuAddress;
        }
    }
}

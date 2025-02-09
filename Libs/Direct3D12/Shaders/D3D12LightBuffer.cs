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

        enum LightBufferType : uint
        {
            NonCullableLight,
            CullableLight,
            CullingInfo,

            Count
        }

        readonly LightBuffer[] buffers;
        ulong currentLightSetKey;

        public D3D12LightBuffer()
        {
            buffers = new LightBuffer[(uint)LightBufferType.Count];
            for (int i = 0; i < buffers.Length; i++)
            {
                buffers[i] = new();
            }

            currentLightSetKey = 0;
        }

        public void UpdateLightBuffers(LightSet set, ulong lightSetKey, uint frameIndex)
        {
            uint[] sizes = new uint[(uint)LightBufferType.Count];
            sizes[(uint)LightBufferType.NonCullableLight] = set.NonCullableLightCount() * (uint)Marshal.SizeOf<DirectionalLightParameters>();
            sizes[(uint)LightBufferType.CullableLight] = set.CullableLightCount() * (uint)Marshal.SizeOf<LightParameters>();
            sizes[(uint)LightBufferType.CullingInfo] = set.CullableLightCount() * (uint)Marshal.SizeOf<LightCullingLightInfo>();

            uint[] currentSizes = new uint[(uint)LightBufferType.Count];
            currentSizes[(uint)LightBufferType.NonCullableLight] = buffers[(uint)LightBufferType.NonCullableLight].Buffer.Size;
            currentSizes[(uint)LightBufferType.CullableLight] = buffers[(uint)LightBufferType.CullableLight].Buffer.Size;
            currentSizes[(uint)LightBufferType.CullingInfo] = buffers[(uint)LightBufferType.CullingInfo].Buffer.Size;

            if (currentSizes[(uint)LightBufferType.NonCullableLight] < sizes[(uint)LightBufferType.NonCullableLight])
            {
                ResizeBuffer(LightBufferType.NonCullableLight, sizes[(uint)LightBufferType.NonCullableLight], frameIndex);
            }

            set.NonCullableLights(out var lights);
            buffers[(uint)LightBufferType.NonCullableLight].Write(lights);

            // Update cullable light buffers
            bool buffersResized = false;
            if (currentSizes[(uint)LightBufferType.CullableLight] < sizes[(uint)LightBufferType.CullableLight])
            {
                Debug.Assert(currentSizes[(uint)LightBufferType.CullingInfo] < sizes[(uint)LightBufferType.CullingInfo]);
                ResizeBuffer(LightBufferType.CullableLight, sizes[(uint)LightBufferType.CullableLight], frameIndex);
                ResizeBuffer(LightBufferType.CullingInfo, sizes[(uint)LightBufferType.CullingInfo], frameIndex);
                buffersResized = true;
            }

            bool allLightsUpdated = false;
            if (buffersResized || currentLightSetKey != lightSetKey)
            {
                set.CullableLights(out var cullableLights);
                set.CullingInfo(out var cullinginfo);
                buffers[(uint)LightBufferType.CullableLight].Write(cullableLights);
                buffers[(uint)LightBufferType.CullingInfo].Write(cullinginfo);

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
                        Debug.Assert(i * Marshal.SizeOf<LightParameters>() < sizes[(uint)LightBufferType.CullableLight]);
                        Debug.Assert(i * (uint)Marshal.SizeOf<LightCullingLightInfo>() < sizes[(uint)LightBufferType.CullingInfo]);

                        var cullableLight = set.CullableLights(i);
                        var cullingInfo = set.CullingInfo(i);
                        buffers[(uint)LightBufferType.CullableLight].Write(i, cullableLight);
                        buffers[(uint)LightBufferType.CullingInfo].Write(i, cullingInfo);

                        set.SetDirtyBit(i, indexMask);
                    }
                }
            }
        }
        private void ResizeBuffer(LightBufferType type, uint size, uint frameIndex)
        {
            Debug.Assert(type < LightBufferType.Count);
            if (size == 0)
            {
                return;
            }

            buffers[(uint)type].Buffer.Release();
            buffers[(uint)type].Buffer = new(D3D12ConstantBuffer.GetDefaultInitInfo(size), true);

            D3D12Helpers.NameD3D12Object(
                buffers[(uint)type].Buffer.Buffer,
                frameIndex,
                type == LightBufferType.NonCullableLight ? "Non-cullable Light Buffer" :
                type == LightBufferType.CullableLight ? "Cullable Light Buffer" : "Light Culling Info Buffer");

            buffers[(uint)type].Resize();
        }

        public void Release()
        {
            for (uint i = 0; i < (uint)LightBufferType.Count; i++)
            {
                buffers[i].Release();
            }
        }

        public ulong NonCullableLights()
        {
            return buffers[(uint)LightBufferType.NonCullableLight].Buffer.GpuAddress;
        }
        public ulong CullableLights()
        {
            return buffers[(uint)LightBufferType.CullableLight].Buffer.GpuAddress;
        }
        public ulong CullingInfo()
        {
            return buffers[(uint)LightBufferType.CullingInfo].Buffer.GpuAddress;
        }
    }
}

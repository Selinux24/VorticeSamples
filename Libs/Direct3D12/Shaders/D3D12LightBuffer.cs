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
        readonly ulong currentLightSetKey;

        public D3D12LightBuffer()
        {
            buffers = new LightBuffer[(uint)LightBufferType.Count];
            for (int i = 0; i < buffers.Length; i++)
            {
                buffers[i] = new();
            }

            currentLightSetKey = 0;
        }

        unsafe void ResizeBuffer(LightBufferType type, uint size, uint frameIndex)
        {
            Debug.Assert(type < LightBufferType.Count);
            if (size == 0)
            {
                return;
            }

            buffers[(uint)type].Buffer.Release();
            buffers[(uint)type].Buffer = new D3D12Buffer(D3D12ConstantBuffer.GetDefaultInitInfo(size), true);
            D3D12Helpers.NameD3D12Object(
                buffers[(uint)type].Buffer.Buffer,
                frameIndex,
                type == LightBufferType.NonCullableLight ? "Non-cullable Light Buffer" :
                type == LightBufferType.CullableLight ? "Cullable Light Buffer" : "Light Culling Info Buffer");

            fixed (byte** cpuAddress = &buffers[(uint)type].CpuAddress)
            {
                D3D12Helpers.DxCall(buffers[(uint)type].Buffer.Buffer.Map(0, cpuAddress));
                Debug.Assert(buffers[(uint)type].CpuAddress != null);
            }
        }

        public void UpdateLightBuffers(LightSet set, ulong lightSetKey, uint frameIndex)
        {
            uint[] sizes = new uint[(uint)LightBufferType.Count];
            sizes[(uint)LightBufferType.NonCullableLight] = set.NonCullableLightCount() * (uint)Marshal.SizeOf<DirectionalLightParameters>();

            uint[] currentSizes = new uint[(uint)LightBufferType.Count];
            currentSizes[(uint)LightBufferType.NonCullableLight] = buffers[(uint)LightBufferType.NonCullableLight].Buffer.Size;

            if (currentSizes[(uint)LightBufferType.NonCullableLight] < sizes[(uint)LightBufferType.NonCullableLight])
            {
                ResizeBuffer(LightBufferType.NonCullableLight, sizes[(uint)LightBufferType.NonCullableLight], frameIndex);
            }

            set.NonCullableLights(out var lights);
            buffers[(uint)LightBufferType.NonCullableLight].Write(lights);

            // TODO: cullable lights
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
    }
}

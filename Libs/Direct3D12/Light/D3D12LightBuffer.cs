using Direct3D12.Helpers;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Direct3D12.Light
{
    class D3D12LightBuffer : IDisposable
    {
        #region Structures & Enumerations

        struct LightBuffer()
        {
            D3D12Buffer buffer = new();
            IntPtr cpuAddress = IntPtr.Zero;

            public readonly IntPtr CpuAddress => cpuAddress;
            public readonly ulong GpuAddress => buffer.GpuAddress;
            public readonly uint Size => buffer.Size;

            public readonly void Write<T>(T[] lights) where T : unmanaged
            {
                Debug.Assert(cpuAddress != IntPtr.Zero);
                Debug.Assert(buffer.Size >= D3D12Helpers.AlignSizeForConstantBuffer((ulong)(Marshal.SizeOf<T>() * lights.Length)));

                BuffersHelper.WriteUnaligned(lights, cpuAddress);
            }
            public readonly void Write<T>(uint i, T light) where T : unmanaged
            {
                Debug.Assert(cpuAddress != IntPtr.Zero);
                Debug.Assert(buffer.Size >= Marshal.SizeOf<T>() * (i + 1));

                uint offset = (uint)Marshal.SizeOf<T>() * i;
                IntPtr p = cpuAddress + (IntPtr)offset;

                BuffersHelper.WriteUnaligned(light, p);
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
                    type == LightBufferTypes.CullableLight ? "Cullable Light Buffer" :
                    type == LightBufferTypes.CullingInfo ? "Light Culling Info Buffer" :
                    "Bounding Spheres Buffer");

                D3D12Helpers.DxCall(buffer.Buffer.Map(0, out cpuAddress));
                Debug.Assert(cpuAddress != IntPtr.Zero);
            }
            public void Release()
            {
                buffer.Dispose();
                cpuAddress = IntPtr.Zero;
            }
        }

        enum LightBufferTypes : uint
        {
            NonCullableLight,
            CullableLight,
            CullingInfo,
            BoundingSpheres,

            Count,
        }

        #endregion

        readonly LightBuffer[] buffers;
        ulong currentLightSetKey;

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
        void Dispose(bool disposing)
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
            uint nonCullableLightCount = set.NonCullableLightCount();
            if (nonCullableLightCount > 0)
            {
                uint neededSize = nonCullableLightCount * (uint)Marshal.SizeOf<Shaders.DirectionalLightParameters>();
                uint currentSize = buffers[(uint)LightBufferTypes.NonCullableLight].Size;

                if (currentSize < neededSize)
                {
                    buffers[(uint)LightBufferTypes.NonCullableLight].Resize(LightBufferTypes.NonCullableLight, neededSize, frameIndex);
                }

                set.NonCullableLights(out var lights);
                buffers[(uint)LightBufferTypes.NonCullableLight].Write(lights);
            }

            // Update cullable light buffers
            uint cullableLightCount = set.CullableLightCount();
            if (cullableLightCount > 0)
            {
                uint neededLightBufferSize = cullableLightCount * (uint)Marshal.SizeOf<Shaders.LightParameters>();
                uint neededCullingBufferSize = cullableLightCount * (uint)Marshal.SizeOf<Shaders.LightCullingLightInfo>();
                uint neededSpheresBufferSize = cullableLightCount * (uint)Marshal.SizeOf<Shaders.Sphere>();
                uint currentLightBufferSize = buffers[(uint)LightBufferTypes.CullableLight].Size;

                bool buffersResized = false;
                if (currentLightBufferSize < neededLightBufferSize)
                {
                    // NOTE: we create buffers about 150% larger than needed to avoid recreating them
                    //       everytime a few lights are added.
                    buffers[(uint)LightBufferTypes.CullableLight].Resize(LightBufferTypes.CullableLight, (neededLightBufferSize * 3) >> 1, frameIndex);
                    buffers[(uint)LightBufferTypes.CullingInfo].Resize(LightBufferTypes.CullingInfo, (neededCullingBufferSize * 3) >> 1, frameIndex);
                    buffers[(uint)LightBufferTypes.BoundingSpheres].Resize(LightBufferTypes.BoundingSpheres, (neededSpheresBufferSize * 3) >> 1, frameIndex);
                    buffersResized = true;
                }

                uint indexMask = (uint)(1 << (int)frameIndex);

                if (buffersResized || currentLightSetKey != lightSetKey)
                {
                    set.CullableLights(out var cullableLights);
                    set.CullingInfo(out var cullinginfo);
                    set.BoundingSpheres(out var boundingSpheres);
                    buffers[(uint)LightBufferTypes.CullableLight].Write(cullableLights);
                    buffers[(uint)LightBufferTypes.CullingInfo].Write(cullinginfo);
                    buffers[(uint)LightBufferTypes.BoundingSpheres].Write(boundingSpheres);
                    currentLightSetKey = lightSetKey;

                    for (uint i = 0; i < cullableLightCount; i++)
                    {
                        set.ClearDirtyBit(i, indexMask);
                    }
                }
                else if (set.SomethingIsDirty)
                {
                    for (uint i = 0; i < cullableLightCount; i++)
                    {
                        if (!set.GetDirtyBit(i, indexMask))
                        {
                            continue;
                        }

                        Debug.Assert(i * Marshal.SizeOf<Shaders.LightParameters>() < neededLightBufferSize);
                        Debug.Assert(i * Marshal.SizeOf<Shaders.LightCullingLightInfo>() < neededCullingBufferSize);
                        Debug.Assert(i * Marshal.SizeOf<Shaders.Sphere>() < neededSpheresBufferSize);

                        var cullableLight = set.CullableLights(i);
                        var cullingInfo = set.CullingInfo(i);
                        var boundingSphere = set.BoundingSphere(i);
                        buffers[(uint)LightBufferTypes.CullableLight].Write(i, cullableLight);
                        buffers[(uint)LightBufferTypes.CullingInfo].Write(i, cullingInfo);
                        buffers[(uint)LightBufferTypes.BoundingSpheres].Write(i, boundingSphere);

                        set.ClearDirtyBit(i, indexMask);
                    }
                }

                set.ClearDirty(indexMask);
                Debug.Assert(currentLightSetKey == lightSetKey);
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
        public ulong BoundingSpheres()
        {
            return buffers[(uint)LightBufferTypes.BoundingSpheres].GpuAddress;
        }
    }
}

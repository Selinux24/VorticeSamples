using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Vortice.Direct3D12;

namespace Direct3D12.Shaders
{
    class D3D12ConstantBuffer(D3D12BufferInitInfo info) : IDisposable
    {
        unsafe struct ConstantBuffer
        {
            private readonly object mutex = new();
            private readonly D3D12Buffer buffer;
            private byte* cpuAddress;
            private uint cpuOffset;

            public readonly ID3D12Resource Buffer => buffer.Buffer;
            public readonly ulong GpuAddress => buffer.GpuAddress;
            public readonly uint Size => buffer.Size;

            public ConstantBuffer(D3D12BufferInitInfo info)
            {
                buffer = new(info, true);

                D3D12Helpers.NameD3D12Object(buffer.Buffer, buffer.Size, "Constant Buffer - size");

                fixed (byte** cpuAddress = &this.cpuAddress)
                {
                    D3D12Helpers.DxCall(buffer.Buffer.Map(0, cpuAddress));
                    Debug.Assert(this.cpuAddress != null);
                }
            }

            public ulong Write<T>(T data) where T : unmanaged
            {
                // NOTE: be careful not to read from this buffer. Reads are really really slow.
                T* p = Allocate<T>();
                // TODO: handle the case when cbuffer is full.
                BuffersHelper.Write(p, data);

                return GetGpuAddress(p);
            }
            private T* Allocate<T>() where T : unmanaged
            {
                lock (mutex)
                {
                    uint size = (uint)Marshal.SizeOf<T>();
                    uint alignedSize = (uint)D3D12Helpers.AlignSizeForConstantBuffer(size);
                    Debug.Assert(cpuOffset + alignedSize <= buffer.Size);
                    if (cpuOffset + alignedSize <= buffer.Size)
                    {
                        byte* address = cpuAddress + cpuOffset;
                        cpuOffset += alignedSize;
                        return (T*)address;
                    }

                    return null;
                }
            }
            private readonly ulong GetGpuAddress<T>(T* allocation) where T : unmanaged
            {
                lock (mutex)
                {
                    Debug.Assert(cpuAddress != null);
                    if (cpuAddress == null)
                    {
                        return 0;
                    }

                    byte* address = (byte*)allocation;

                    Debug.Assert(address <= cpuAddress + cpuOffset);
                    Debug.Assert(address >= cpuAddress);
                    ulong offset = (ulong)(address - cpuAddress);
                    return buffer.GpuAddress + offset;
                }
            }
            public void Clear()
            {
                cpuOffset = 0;
            }
            public void Release()
            {
                buffer.Release();
                cpuAddress = null;
                cpuOffset = 0;
            }
        }

        private ConstantBuffer cBuffer = new(info);

        public ID3D12Resource Buffer { get => cBuffer.Buffer; }
        public ulong GpuAddress { get => cBuffer.GpuAddress; }
        public uint Size { get => cBuffer.Size; }

        ~D3D12ConstantBuffer()
        {
            Release();
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
                Release();
            }
        }
        public void Release()
        {
            cBuffer.Release();
        }

        public void Clear()
        {
            cBuffer.Clear();
        }

        public ulong Write<T>(T data) where T : unmanaged
        {
            return cBuffer.Write(data);
        }

        public static D3D12BufferInitInfo GetDefaultInitInfo(uint size)
        {
            Debug.Assert(size > 0);

            return new()
            {
                Size = size,
                Alignment = D3D12.ConstantBufferDataPlacementAlignment
            };
        }
    }
}

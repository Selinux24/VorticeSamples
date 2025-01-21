using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Direct3D12;

namespace Direct3D12
{
    unsafe class ConstantBuffer : IDisposable
    {
        private readonly D3D12Buffer buffer;
        private byte* cpuAddress = null;
        private uint cpuOffset = 0;
        private readonly object mutex = new();

        public ID3D12Resource Buffer { get => buffer.Buffer; }
        public ulong GpuAddress { get => buffer.GpuAddress; }
        public uint Size { get => buffer.Size; }

        public ConstantBuffer(D3D12BufferInitInfo info)
        {
            buffer = new(info, true);

            D3D12Helpers.NameD3D12Object(Buffer, Size, "Constant Buffer - size");

            fixed (byte** cpuAddress = &this.cpuAddress)
            {
                D3D12Helpers.DxCall(Buffer.Map(0, cpuAddress));
                Debug.Assert(this.cpuAddress != null);
            }
        }
        ~ConstantBuffer()
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
            buffer.Release();
            cpuAddress = null;
            cpuOffset = 0;
        }

        public void Clear()
        {
            cpuOffset = 0;
        }
      
        public ulong Write<T>(T data) where T : unmanaged
        {
            uint size = (uint)Marshal.SizeOf<T>();

            // NOTE: be careful not to read from this buffer. Reads are really really slow.
            T* p = (T*)Allocate(size);
            // TODO: handle the case when cbuffer is full.
            Unsafe.CopyBlockUnaligned(p, Unsafe.AsPointer(ref data), size);

            return GetGpuAddress(p);
        }
        private byte* Allocate(uint size)
        {
            lock (mutex)
            {
                uint alignedSize = (uint)D3D12Helpers.AlignSizeForConstantBuffer(size);
                Debug.Assert(cpuOffset + alignedSize <= buffer.Size);
                if (cpuOffset + alignedSize <= buffer.Size)
                {
                    var address = cpuAddress + cpuOffset;
                    cpuOffset += alignedSize;
                    return address;
                }

                return null;
            }
        }
        private ulong GetGpuAddress<T>(T* allocation) where T : unmanaged
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

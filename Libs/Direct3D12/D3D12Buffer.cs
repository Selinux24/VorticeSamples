using System;
using System.Diagnostics;
using Vortice.Direct3D12;
using Vortice.Mathematics;

namespace Direct3D12
{
    class D3D12Buffer : IDisposable
    {
        ID3D12Resource buffer = null;
        ulong gpuAddress = 0;
        uint size = 0;

        public ID3D12Resource Buffer { get => buffer; }
        public ulong GpuAddress { get => gpuAddress; }
        public uint Size { get => size; }

        public D3D12Buffer Set(D3D12Buffer o)
        {
            Debug.Assert(this != o);
            if (this != o)
            {
                Release();
                Move(o);
            }

            return this;
        }
        public D3D12Buffer(D3D12BufferInitInfo info, bool isCpuAccessible)
        {
            Debug.Assert(buffer == null && info.Size > 0 && info.Alignment > 0);
            size = MathHelper.AlignUp(info.Size, info.Alignment);
            buffer = D3D12Helpers.CreateBuffer(info.Data, size, isCpuAccessible, info.InitialState, info.Flags, info.Heap, info.AllocationInfo.Offset);
            gpuAddress = buffer.GPUVirtualAddress;
            D3D12Helpers.NameD3D12Object(buffer, size, "D3D12 Buffer - size");
        }

        public D3D12Buffer(D3D12Buffer o)
        {
            buffer = o.buffer;
            gpuAddress = o.gpuAddress;
            size = o.size;
            o.Reset();
        }
        ~D3D12Buffer()
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
            D3D12Graphics.DeferredRelease(buffer);
            gpuAddress = 0;
            size = 0;
        }

        private void Move(D3D12Buffer o)
        {
            buffer = o.buffer;
            gpuAddress = o.gpuAddress;
            size = o.size;
            o.Reset();
        }
        private void Reset()
        {
            buffer = null;
            gpuAddress = 0;
            size = 0;
        }
    }
}

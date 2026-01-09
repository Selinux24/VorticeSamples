using System;
using Vortice.Direct3D12;

namespace Direct3D12
{
    class D3D12Buffer() : IDisposable
    {
        readonly ID3D12Resource buffer = null;
        ulong gpuAddress = 0;
        uint size = 0;

        public ID3D12Resource Buffer { get => buffer; }
        public ulong GpuAddress { get => gpuAddress; }
        public uint Size { get => size; }

        public D3D12Buffer(D3D12BufferInitInfo info, bool isCpuAccessible) : this()
        {
            buffer = D3D12Helpers.CreateBuffer(info.Data, info.AlignedSize, isCpuAccessible, info.InitialState, info.Flags, info.Heap, info.AllocationInfo.Offset);
            gpuAddress = buffer.GPUVirtualAddress;
            size = info.AlignedSize;
            D3D12Helpers.NameD3D12Object(buffer, size, "D3D12 Buffer - size");
        }
        ~D3D12Buffer()
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
                D3D12Graphics.DeferredRelease(buffer);
                gpuAddress = 0;
                size = 0;
            }
        }
    }
}

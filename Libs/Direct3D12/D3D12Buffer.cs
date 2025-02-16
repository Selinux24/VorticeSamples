using System;
using System.Diagnostics;
using Vortice.Direct3D12;
using Vortice.Mathematics;

namespace Direct3D12
{
    class D3D12Buffer : IDisposable
    {
        private readonly ID3D12Resource buffer = null;
        private ulong gpuAddress = 0;
        private uint size = 0;

        public ID3D12Resource Buffer { get => buffer; }
        public ulong GpuAddress { get => gpuAddress; }
        public uint Size { get => size; }

        public D3D12Buffer(D3D12BufferInitInfo info, bool isCpuAccessible)
        {
            Debug.Assert(buffer == null && info.Size > 0 && info.Alignment > 0);
            size = MathHelper.AlignUp(info.Size, info.Alignment);
            buffer = D3D12Helpers.CreateBuffer(info.Data, size, isCpuAccessible, info.InitialState, info.Flags, info.Heap, info.AllocationInfo.Offset);
            gpuAddress = buffer.GPUVirtualAddress;
            D3D12Helpers.NameD3D12Object(buffer, size, "D3D12 Buffer - size");
        }

        public D3D12Buffer()
        {
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
        private void Dispose(bool disposing)
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

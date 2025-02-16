using System;
using System.Diagnostics;
using Vortice.Direct3D12;
using Vortice.Mathematics;

namespace Direct3D12
{
    class StructuredBuffer : IDisposable
    {
        private readonly D3D12Buffer buffer;
        private DescriptorHandle uav;
        private DescriptorHandle uavShaderVisible;
        private readonly uint stride;

        public ID3D12Resource Buffer => buffer.Buffer;
        public ulong GpuAddress => buffer.GpuAddress;
        public uint Size => buffer.Size;
        public uint Stride => stride;
        public DescriptorHandle Uav => uav;
        public DescriptorHandle UavShaderVisible => uavShaderVisible;

        public StructuredBuffer(D3D12BufferInitInfo info)
        {
            buffer = new(info, false);
            stride = info.Stride;

            Debug.Assert(info.Size > 0 && info.Size == info.Stride * info.ElementCount);
            Debug.Assert(info.Alignment > 0);
            D3D12Helpers.NameD3D12Object(buffer.Buffer, buffer.Size, "Structured Buffer - size");

            if (info.CreateUav)
            {
                Debug.Assert((info.Flags & ResourceFlags.AllowUnorderedAccess) != 0);
                uav = D3D12Graphics.UavHeap.Allocate();
                uavShaderVisible = D3D12Graphics.SrvHeap.Allocate();
                UnorderedAccessViewDescription desc = new();
                desc.ViewDimension = UnorderedAccessViewDimension.Buffer;
                desc.Format = Vortice.DXGI.Format.R32_UInt;
                desc.Buffer.CounterOffsetInBytes = 0;
                desc.Buffer.FirstElement = 0;
                desc.Buffer.Flags = BufferUnorderedAccessViewFlags.None;
                desc.Buffer.NumElements = buffer.Size / sizeof(uint);

                D3D12Graphics.Device.CreateUnorderedAccessView(buffer.Buffer, null, desc, uav.Cpu);
                D3D12Graphics.Device.CopyDescriptorsSimple(1, uavShaderVisible.Cpu, uav.Cpu, D3D12Graphics.SrvHeap.DescriptorType);
            }
        }

        ~StructuredBuffer()
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
                D3D12Graphics.SrvHeap.Free(ref uavShaderVisible);
                D3D12Graphics.UavHeap.Free(ref uav);
                buffer.Dispose();
            }
        }

        public void ClearUav(ID3D12GraphicsCommandList cmdList, Int4 clearValue)
        {
            cmdList.ClearUnorderedAccessViewUint(uavShaderVisible.Gpu, uav.Cpu, buffer.Buffer, clearValue);
        }
        public void ClearUav(ID3D12GraphicsCommandList cmdList, Color4 clearValue)
        {
            cmdList.ClearUnorderedAccessViewFloat(uavShaderVisible.Gpu, uav.Cpu, buffer.Buffer, clearValue);
        }

        public static D3D12BufferInitInfo GetDefaultInitInfo(uint stride, uint elementCount)
        {
            Debug.Assert(stride > 0 && elementCount > 0);

            return new()
            {
                Size = stride * elementCount,
                Stride = stride,
                ElementCount = elementCount,
                Alignment = stride,
                Flags = ResourceFlags.AllowUnorderedAccess
            };
        }
    }
}

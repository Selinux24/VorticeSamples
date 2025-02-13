using System;
using System.Diagnostics;
using Vortice.Direct3D12;
using Vortice.Mathematics;

namespace Direct3D12.Shaders
{
    class D3D12StructuredBuffer(D3D12BufferInitInfo info) : IDisposable
    {
        struct StructuredBuffer
        {
            private readonly D3D12Buffer buffer;

            public readonly ID3D12Resource Buffer => buffer.Buffer;
            public readonly ulong GpuAddress => buffer.GpuAddress;
            public readonly uint Size => buffer.Size;
            public readonly uint Stride;
            public D3D12DescriptorHandle Uav;
            public D3D12DescriptorHandle UavShaderVisible;

            public StructuredBuffer(D3D12BufferInitInfo info)
            {
                buffer = new(info, false);
                Stride = info.Stride;

                Debug.Assert(info.Size > 0 && info.Size == (info.Stride * info.ElementCount));
                Debug.Assert(info.Alignment > 0);
                D3D12Helpers.NameD3D12Object(buffer.Buffer, buffer.Size, "Structured Buffer - size");

                if (info.CreateUav)
                {
                    Debug.Assert((info.Flags & ResourceFlags.AllowUnorderedAccess) != 0);
                    Uav = D3D12Graphics.UavHeap.Allocate();
                    UavShaderVisible = D3D12Graphics.SrvHeap.Allocate();
                    UnorderedAccessViewDescription desc = new();
                    desc.ViewDimension = UnorderedAccessViewDimension.Buffer;
                    desc.Format = Vortice.DXGI.Format.R32_UInt;
                    desc.Buffer.CounterOffsetInBytes = 0;
                    desc.Buffer.FirstElement = 0;
                    desc.Buffer.Flags = BufferUnorderedAccessViewFlags.None;
                    desc.Buffer.NumElements = buffer.Size / sizeof(uint);

                    D3D12Graphics.Device.CreateUnorderedAccessView(buffer.Buffer, null, desc, Uav.Cpu);
                    D3D12Graphics.Device.CopyDescriptorsSimple(1, UavShaderVisible.Cpu, Uav.Cpu, D3D12Graphics.SrvHeap.DescriptorType);
                }
            }

            public void Release()
            {
                D3D12Graphics.SrvHeap.Free(ref UavShaderVisible);
                D3D12Graphics.UavHeap.Free(ref Uav);
                buffer.Release();
            }

            public readonly void ClearUav(ID3D12GraphicsCommandList cmdList, Int4 clearValue)
            {
                cmdList.ClearUnorderedAccessViewUint(UavShaderVisible.Gpu, Uav.Cpu, buffer.Buffer, clearValue);
            }
            public readonly void ClearUav(ID3D12GraphicsCommandList cmdList, Color4 clearValue)
            {
                cmdList.ClearUnorderedAccessViewFloat(UavShaderVisible.Gpu, Uav.Cpu, buffer.Buffer, clearValue);
            }
        }

        private StructuredBuffer sBuffer = new(info);

        public ID3D12Resource Buffer { get => sBuffer.Buffer; }
        public ulong GpuAddress { get => sBuffer.GpuAddress; }
        public uint Size { get => sBuffer.Size; }
        public D3D12DescriptorHandle Uav { get => sBuffer.Uav; }
        public D3D12DescriptorHandle UavShaderVisible { get => sBuffer.UavShaderVisible; }

        ~D3D12StructuredBuffer()
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
            sBuffer.Release();
        }

        public void ClearUav(ID3D12GraphicsCommandList cmdList, Int4 clearValue)
        {
            sBuffer.ClearUav(cmdList, clearValue);
        }
        public void ClearUav(ID3D12GraphicsCommandList cmdList, Color4 clearValue)
        {
            sBuffer.ClearUav(cmdList, clearValue);
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

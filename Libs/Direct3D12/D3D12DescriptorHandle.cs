using Vortice.Direct3D12;

namespace Direct3D12
{
    struct D3D12DescriptorHandle()
    {
        public CpuDescriptorHandle Cpu;
        public GpuDescriptorHandle Gpu;
        public D3D12DescriptorHeap Container;
        public uint Index = uint.MaxValue;

        public readonly bool IsValid()
        {
            return Cpu.Ptr != 0;
        }
        public readonly bool IsShaderVisible()
        {
            return Gpu.Ptr != 0;
        }
    }
}

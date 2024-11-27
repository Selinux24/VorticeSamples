using Vortice.Direct3D12;

namespace Direct3D12
{
    struct DescriptorHandle()
    {
        public CpuDescriptorHandle Cpu;
        public GpuDescriptorHandle Gpu;
        public DescriptorHeap Container;
        public nint Index = nint.MaxValue;

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

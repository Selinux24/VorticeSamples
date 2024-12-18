using Vortice.Direct3D12;

namespace Direct3D12
{
    static class D3D12Helpers
    {
        public static readonly HeapProperties DefaultHeap = new(
            HeapType.Default,
            CpuPageProperty.Unknown,
            MemoryPool.Unknown,
            0,
            0);
    }
}

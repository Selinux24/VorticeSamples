using Vortice.Direct3D12;

namespace Direct3D12
{
    struct D3D12TextureInitInfo()
    {
        public ID3D12Heap1 Heap = null;
        public ID3D12Resource Resource = null;
        public ShaderResourceViewDescription SrvDesc = default;
        public ResourceDescription Desc = default;
        public ResourceAllocationInfo1 AllocationInfo = default;
        public ResourceStates InitialState = ResourceStates.None;
        public ClearValue ClearValue = default;
    }
}

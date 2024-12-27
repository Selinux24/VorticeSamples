using Vortice.Direct3D12;

namespace Direct3D12
{
    /// <summary>
    /// Represents a D3D12 texture initialization information.
    /// </summary>
    struct D3D12TextureInitInfo()
    {
        public ID3D12Heap1 Heap = null;
        public ID3D12Resource Resource = null;
        public ShaderResourceViewDescription? SrvDesc = null;
        public ResourceDescription? Desc = null;
        public ResourceAllocationInfo1 AllocationInfo = default;
        public ResourceStates InitialState = ResourceStates.None;
        public ClearValue ClearValue = default;
    }
}

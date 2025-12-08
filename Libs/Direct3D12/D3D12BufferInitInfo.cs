using Vortice.Direct3D12;

namespace Direct3D12
{
    struct D3D12BufferInitInfo()
    {
        public ID3D12Heap1 Heap = null;
        public byte[] Data = null;
        public ResourceAllocationInfo1 AllocationInfo = new();
        public ResourceStates InitialState = ResourceStates.Common;
        public ResourceFlags Flags = ResourceFlags.None;
        public uint Size = 0;
        public uint Alignment = 0;
    }
}

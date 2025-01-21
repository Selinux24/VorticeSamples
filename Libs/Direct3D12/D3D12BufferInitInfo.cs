using Vortice.Direct3D12;

namespace Direct3D12
{
    struct D3D12BufferInitInfo
    {
        public ID3D12Heap1 Heap;
        public byte[] Data;
        public ResourceAllocationInfo1 AllocationInfo;
        public ResourceStates InitialState;
        public ResourceFlags Flags;
        public uint Size;
        public uint Stride;
        public uint ElementCount;
        public uint Alignment;
        public bool CreateUav;
    }
}

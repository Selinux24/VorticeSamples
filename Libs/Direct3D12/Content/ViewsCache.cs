using Vortice.Direct3D;
using Vortice.Direct3D12;

namespace Direct3D12.Content
{
    struct ViewsCache
    {
        public ulong[] PositionBuffers;
        public ulong[] ElementBuffers;
        public IndexBufferView[] IndexBufferViews;
        public PrimitiveTopology[] PrimitiveTopologies;
        public uint[] ElementsTypes;
    }
}

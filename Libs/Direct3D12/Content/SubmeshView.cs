using Vortice.Direct3D;
using Vortice.Direct3D12;

namespace Direct3D12.Content
{
    struct SubmeshView
    {
        public VertexBufferView PositionBufferView;
        public VertexBufferView ElementBufferView;
        public IndexBufferView IndexBufferView;
        public PrimitiveTopology PrimitiveTopology;
        public uint ElementsType;
    }
}

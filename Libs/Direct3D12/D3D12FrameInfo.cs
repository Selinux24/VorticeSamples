using PrimalLike.Graphics;

namespace Direct3D12
{
    public struct D3D12FrameInfo()
    {
        public FrameInfo FrameInfo = default;
        public D3D12Camera Camera = null;
        public ulong GlobalShaderData = 0;
        public uint SurfaceWidth = 0;
        public uint SurfaceHeight = 0;
        public uint LightCullingId = uint.MaxValue;
        public uint FrameIndex = 0;
    }
}

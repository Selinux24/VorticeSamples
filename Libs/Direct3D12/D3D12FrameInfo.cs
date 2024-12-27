using PrimalLike.Graphics;

namespace Direct3D12
{
    public struct D3D12FrameInfo : IFrameInfo
    {
        public int SurfaceWidth { get; set; }
        public int SurfaceHeight { get; set; }
    }
}

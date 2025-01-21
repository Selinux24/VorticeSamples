using PrimalLike.Graphics;

namespace Direct3D12
{
    public struct D3D12FrameInfo
    {
        public FrameInfo FrameInfo;
        public D3D12Camera Camera;
        public ulong GlobalShaderData;
        public uint SurfaceWidth;
        public uint SurfaceHeight;
        public uint FrameIndex;
        public float DeltaTime;
    }
}


namespace PrimalLike.Graphics
{
    public struct FrameInfo()
    {
        public IdType[] RenderItemIds = null;
        public float[] Thresholds = null;
        public ulong LightSetKey = 0;
        public float LastFrameTime = 16.7f;
        public float AverageFrameTime = 16.7f;
        public uint RenderItemCount = 0;
        public CameraId CameraId = CameraId.MaxValue;
    }
}

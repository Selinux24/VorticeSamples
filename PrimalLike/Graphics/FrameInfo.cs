
namespace PrimalLike.Graphics
{
    public struct FrameInfo()
    {
        public IdType[] RenderItemIds = null;
        public float[] Thresholds = null;
        public uint RenderItemCount = 0;
        public CameraId CameraId = CameraId.MaxValue;
    }
}

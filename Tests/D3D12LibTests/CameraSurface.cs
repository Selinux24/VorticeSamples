using PrimalLike;
using PrimalLike.EngineAPI;
using PrimalLike.Graphics;
using PrimalLike.Platform;

namespace D3D12LibTests
{
    class CameraSurface(IPlatformWindowInfo info) : RenderComponent(info)
    {
        private FrameInfo frameInfo = new();

        public Camera Camera { get; set; }
        public Entity Entity { get; set; }

        public override void CreateCamera(Entity entity)
        {
            Entity = entity;
            Camera = Application.CreateCamera(new PerspectiveCameraInitInfo(Entity.Id));
            Camera.AspectRatio = (float)Surface.Window.Width / Surface.Window.Height;
        }

        public void UpdateFrameInfo(uint[] items, float[] thresholds, ulong lightSetKey)
        {
            frameInfo.CameraId = Camera.Id;
            frameInfo.RenderItemIds = items;
            frameInfo.RenderItemCount = (uint)items.Length;
            frameInfo.Thresholds = thresholds;
            frameInfo.LightSetKey = lightSetKey;
        }

        public override FrameInfo GetFrameInfo(Time time)
        {
            frameInfo.LastFrameTime = time.DeltaTime;
            frameInfo.AverageFrameTime = time.AverageDeltaTime;

            return frameInfo;
        }
        public override void Remove()
        {
            Application.RemoveRenderSurface(Surface);
            Application.RemoveCamera(Camera.Id);
            Application.RemoveEntity(Entity.Id);
        }
    }
}

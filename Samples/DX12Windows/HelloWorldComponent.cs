using PrimalLike;
using PrimalLike.EngineAPI;
using PrimalLike.Graphics;
using WindowsPlatform;

namespace DX12Windows
{
    class HelloWorldComponent(Win32WindowInfo info) : RenderComponent(info)
    {
        private FrameInfo frameInfo = new();
        private readonly float maxTime = 1f;
        private float dt = 0;
        private uint lightSetKey = 0;

        public Camera Camera { get; private set; }
        public Entity Entity { get; private set; }

        public override void CreateCamera(Entity entity)
        {
            Entity = entity;
            Camera = Application.CreateCamera(new PerspectiveCameraInitInfo(Entity.Id));
            Camera.AspectRatio = (float)Surface.Window.Width / Surface.Window.Height;
        }

        public override FrameInfo GetFrameInfo(Time time)
        {
            frameInfo.LastFrameTime = time.DeltaTime;
            frameInfo.AverageFrameTime = time.AverageDeltaTime;

            //LightGenerator.TestLights(time.DeltaTime);

            dt += time.DeltaTime;
            if (dt > maxTime)
            {
                lightSetKey = (lightSetKey + 1) % 2;
                dt %= maxTime;
            }
            //frameInfo.LightSetKey = lightSetKey;

            return frameInfo;
        }
        public override void Remove()
        {
            Application.RemoveRenderSurface(Surface);
            Application.RemoveCamera(Camera.Id);
            Application.RemoveEntity(Entity.Id);
        }

        public void UpdateFrameInfo(uint[] items, float[] thresholds)
        {
            frameInfo.CameraId = Camera.Id;
            frameInfo.RenderItemIds = items;
            frameInfo.RenderItemCount = (uint)items.Length;
            frameInfo.Thresholds = thresholds;
            frameInfo.LightSetKey = 0;
        }
    }
}

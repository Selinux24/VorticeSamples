using PrimalLike;
using PrimalLike.Components;
using PrimalLike.EngineAPI;
using PrimalLike.Graphics;
using System.Numerics;
using WindowsPlatform;

namespace DX12Windows
{
    class HelloWorldComponent : RenderComponent
    {
        private FrameInfo frameInfo = new();

        public Camera Camera { get; set; }
        public Entity Entity { get; set; }

        public HelloWorldComponent(Win32WindowInfo info) : base(info)
        {
            Surface = Application.CreateRenderSurface(info);

            EntityInfo entityInfo = new()
            {
                Transform = new()
                {
                    Rotation = Quaternion.CreateFromYawPitchRoll(0, 3.14f, 0),
                    Position = new Vector3(0, 1f, 3f),
                },
            };

            Entity = Application.CreateEntity(entityInfo);
            Camera = Application.CreateCamera(new PerspectiveCameraInitInfo(Entity.Id));
            Camera.AspectRatio = (float)Surface.Window.Width / Surface.Window.Height;
        }

        public override FrameInfo GetFrameInfo()
        {
            return frameInfo;
        }
        public override void Resized()
        {
            Surface.Surface.Resize(Surface.Window.Width, Surface.Window.Height);
            Camera.AspectRatio = (float)Surface.Window.Width / Surface.Window.Height;
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

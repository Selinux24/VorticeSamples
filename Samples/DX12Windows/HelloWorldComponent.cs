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

            frameInfo.CameraId = Camera.Id;
            frameInfo.RenderItemIds = [];
            frameInfo.RenderItemCount = 0;
            frameInfo.Thresholds = [];
        }

        public override FrameInfo GetFrameInfo()
        {
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

using Engine;
using Engine.Graphics;
using Engine.Platform;

namespace DX12Windows
{
    class HelloWorldApp(IPlatformFactory platformFactory, IGraphicsFactory graphicsFactory) : Application(platformFactory, graphicsFactory)
    {
        public static HelloWorldApp Start<TPlatform, TGraphics>()
            where TPlatform : IPlatformFactory, new()
            where TGraphics : IGraphicsFactory, new()
        {
            return new HelloWorldApp(new TPlatform(), new TGraphics());
        }

        protected override void Initialize()
        {
            Core.EngineInitialize("Content/Game.bin");
        }
        protected override void Update(Time time)
        {
            Core.EngineUpdate(time.DeltaTime);
        }
        protected override void Shutdown()
        {
            Core.EngineShutdown();
        }
    }
}

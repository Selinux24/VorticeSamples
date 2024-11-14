using Engine;
using Engine.Graphics;
using Engine.Platform;

namespace DX12Windows
{
    class HelloWorldApp(IPlatformFactory platformFactory, PlatformWindowInfo windowInfo, IGraphicsFactory graphicsFactory) : Application(platformFactory, windowInfo, graphicsFactory)
    {
        public static HelloWorldApp Start<TPlatform, TGraphics>(PlatformWindowInfo windowInfo)
            where TPlatform : IPlatformFactory, new()
            where TGraphics : IGraphicsFactory, new()
        {
            var app = new HelloWorldApp(new TPlatform(), windowInfo, new TGraphics());

            return app;
        }

        protected override void Initialize()
        {
            base.Initialize();

            Engine.Core.Engine.EngineInitialize("Content/Game.bin", MainWindow);
        }
        protected override void Update(Time time)
        {
            base.Update(time);

            Engine.Core.Engine.EngineUpdate(time.DeltaTime);
        }
        protected override void Shutdown()
        {
            Engine.Core.Engine.EngineShutdown();
        }
    }
}

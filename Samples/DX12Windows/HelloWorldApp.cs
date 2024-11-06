using Engine;
using Engine.Graphics;

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
            base.Initialize();

            Engine.Core.Engine.EngineInitialize("Content/Game.bin");
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

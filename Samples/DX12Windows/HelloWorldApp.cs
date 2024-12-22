using PrimalLike;
using PrimalLike.Graphics;
using PrimalLike.Platform;

namespace DX12Windows
{
    class HelloWorldApp(IPlatformFactory platformFactory, IGraphicsPlatformFactory graphicsFactory) : Application(platformFactory, graphicsFactory)
    {
        public static HelloWorldApp Start<TPlatform, TGraphics>()
            where TPlatform : IPlatformFactory, new()
            where TGraphics : IGraphicsPlatformFactory, new()
        {
            return new HelloWorldApp(new TPlatform(), new TGraphics());
        }

        protected override void Initialize()
        {
            Engine.EngineInitialize("Content/Game.bin");
        }
        protected override void Update(Time time)
        {
            Engine.EngineUpdate(time.DeltaTime);
        }
        protected override void Shutdown()
        {
            Engine.EngineShutdown();
        }
    }
}

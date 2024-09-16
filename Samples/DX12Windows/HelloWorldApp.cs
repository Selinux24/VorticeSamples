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
    }
}

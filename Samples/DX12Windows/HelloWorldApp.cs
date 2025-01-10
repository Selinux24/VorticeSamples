using PrimalLike;
using PrimalLike.Graphics;
using PrimalLike.Platform;

namespace DX12Windows
{
    class HelloWorldApp(IPlatformFactory platformFactory, IGraphicsPlatformFactory graphicsFactory)
        : Application("Content/Game.bin", platformFactory, graphicsFactory)
    {
        public static HelloWorldApp Start<TPlatform, TGraphics>()
            where TPlatform : IPlatformFactory, new()
            where TGraphics : IGraphicsPlatformFactory, new()
        {
            return new HelloWorldApp(new TPlatform(), new TGraphics());
        }
    }
}

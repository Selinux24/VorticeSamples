using PrimalLike;
using PrimalLike.Graphics;
using PrimalLike.Platform;

namespace D3D12LibTests
{
    class TestApp(IPlatformFactory platformFactory, IGraphicsPlatformFactory graphicsFactory)
       : Application("Content/Game.bin", platformFactory, graphicsFactory)
    {
        public static TestApp Start<TPlatform, TGraphics>()
            where TPlatform : IPlatformFactory, new()
            where TGraphics : IGraphicsPlatformFactory, new()
        {
            return new TestApp(new TPlatform(), new TGraphics());
        }
    }
}

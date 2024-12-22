global using SurfaceId = System.UInt32;
using PrimalLike.Platform;

namespace PrimalLike.Graphics
{
    static class Renderer
    {
        private static IGraphicsPlatform gfx;

        private static void SetPlatformInterface(IGraphicsPlatform platform)
        {
            gfx = platform;
        }

        public static bool Initialize(IGraphicsPlatform platform)
        {
            SetPlatformInterface(platform);

            return gfx.Initialize();
        }
        public static void Shutdown()
        {
            gfx.Shutdown();
        }

        public static string GetEngineShaderPath()
        {
            return gfx.GetEngineShaderPath();
        }

        public static ISurface CreateSurface(PlatformWindow window)
        {
            return gfx.CreateSurface(window);
        }
        public static void RemoveSurface(SurfaceId id)
        {
            gfx.RemoveSurface(id);
        }
        public static void ResizeSurface(SurfaceId id, int width, int height)
        {
            gfx.ResizeSurface(id, width, height);
        }
        public static int GetSurfaceWidth(SurfaceId id)
        {
            return gfx.GetSurfaceWidth(id);
        }
        public static int GetSurfaceHeight(SurfaceId id)
        {
            return gfx.GetSurfaceHeight(id);
        }
        public static void RenderSurface(SurfaceId id)
        {
            gfx.RenderSurface(id);
        }
    }
}

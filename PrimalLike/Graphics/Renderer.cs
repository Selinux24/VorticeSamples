global using SurfaceId = System.UInt32;
global using SubmeshId = System.UInt32;
using PrimalLike.Platform;
using System;

namespace PrimalLike.Graphics
{
    public static class Renderer
    {
        private static IGraphicsPlatform gfx;

        private static void SetPlatformInterface(IGraphicsPlatformFactory graphicsFactory)
        {
            gfx = graphicsFactory.CreateGraphicsPlatform();
        }

        public static bool Initialize(IGraphicsPlatformFactory graphicsFactory)
        {
            SetPlatformInterface(graphicsFactory);

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

        public static SubmeshId AddSubmesh(IntPtr data)
        {
            return gfx.AddSubmesh(data);
        }
        public static void RemoveSubmesh(SubmeshId id)
        {
            gfx.RemoveSubmesh(id);
        }
    }
}

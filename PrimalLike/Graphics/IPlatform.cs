using PrimalLike.Platform;

namespace PrimalLike.Graphics
{
    public interface IPlatform
    {
        bool Initialize();
        void Shutdown();
        string GetEngineShaderPath();

        ISurface CreateSurface(PlatformWindow window);
        void RemoveSurface(SurfaceId id);
        void ResizeSurface(SurfaceId id, int width, int height);
        int GetSurfaceWidth(SurfaceId id);
        int GetSurfaceHeight(SurfaceId id);
        void RenderSurface(SurfaceId id);
    }
}

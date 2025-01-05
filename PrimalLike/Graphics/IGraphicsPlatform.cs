using PrimalLike.Platform;
using System;

namespace PrimalLike.Graphics
{
    /// <summary>
    /// Graphics platform interface.
    /// </summary>
    public interface IGraphicsPlatform
    {
        /// <summary>
        /// Initializes the graphics platform.
        /// </summary>
        bool Initialize();
        /// <summary>
        /// Shuts down the graphics platform.
        /// </summary>
        void Shutdown();
        /// <summary>
        /// Gets the engine shaders path.
        /// </summary>
        string GetEngineShaderPath();

        /// <summary>
        /// Creates a surface.
        /// </summary>
        /// <param name="window">Window</param>
        ISurface CreateSurface(PlatformWindow window);
        /// <summary>
        /// Removes a surface.
        /// </summary>
        /// <param name="id">Surface id</param>
        void RemoveSurface(SurfaceId id);
        /// <summary>
        /// Resizes a surface.
        /// </summary>
        /// <param name="id">Surface id</param>
        /// <param name="width">New width</param>
        /// <param name="height">New height</param>
        void ResizeSurface(SurfaceId id, int width, int height);
        /// <summary>
        /// Gets the surface width.
        /// </summary>
        /// <param name="id">Surface id</param>
        int GetSurfaceWidth(SurfaceId id);
        /// <summary>
        /// Gets the surface height.
        /// </summary>
        /// <param name="id">Surface id</param>
        int GetSurfaceHeight(SurfaceId id);
        /// <summary>
        /// Renders a surface.
        /// </summary>
        /// <param name="id">Surface id</param>
        void RenderSurface(SurfaceId id);

        /// <summary>
        /// Adds a submesh.
        /// </summary>
        /// <param name="data">Submesh data</param>
        SubmeshId AddSubmesh(ref IntPtr data);
        /// <summary>
        /// Removes a submesh.
        /// </summary>
        /// <param name="id">Submesh id</param>
        void RemoveSubmesh(SubmeshId id);
    }
}

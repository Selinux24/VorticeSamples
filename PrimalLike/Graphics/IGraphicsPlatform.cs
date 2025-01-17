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
        /// Creates a camera.
        /// </summary>
        /// <param name="info">Camera initialization info</param>
        Camera CreateCamera(CameraInitInfo info);
        /// <summary>
        /// Removes a camera.
        /// </summary>
        /// <param name="id">Camera id</param>
        void RemoveCamera(CameraId id);
        /// <summary>
        /// Sets a camera parameter.
        /// </summary>
        /// <param name="id">Camera id</param>
        /// <param name="parameter">Parameter to set</param>
        /// <param name="data">Data to read-from the parameter value</param>
        /// <param name="size">Size of data</param>
        void SetParameter(CameraId id, CameraParameters parameter, IntPtr data, int size);
        /// <summary>
        /// Gets a camera parameter.
        /// </summary>
        /// <param name="id">Camera id</param>
        /// <param name="parameter">Parameter to get</param>
        /// <param name="data">Data to write-in the parameter value</param>
        /// <param name="size">Size of data</param>
        void GetParameter(CameraId id, CameraParameters parameter, IntPtr data, int size);

        /// <summary>
        /// Adds a submesh.
        /// </summary>
        /// <param name="data">Submesh data</param>
        IdType AddSubmesh(ref IntPtr data);
        /// <summary>
        /// Removes a submesh.
        /// </summary>
        /// <param name="id">Submesh id</param>
        void RemoveSubmesh(IdType id);
        /// <summary>
        /// Adds a material.
        /// </summary>
        /// <param name="info">Material info</param>
        IdType AddMaterial(MaterialInitInfo info);
        /// <summary>
        /// Removes a material.
        /// </summary>
        /// <param name="id">Material id</param>
        void RemoveMaterial(IdType id);
        /// <summary>
        /// Adds a render item.
        /// </summary>
        /// <param name="entityId">Entity id</param>
        /// <param name="geometryContentId">Geometry id</param>
        /// <param name="materialCount">Material count</param>
        /// <param name="materialIds">Material id list</param>
        IdType AddRenderItem(IdType entityId, IdType geometryContentId, uint materialCount, IdType[] materialIds);
        /// <summary>
        /// Removes a render item.
        /// </summary>
        /// <param name="id">Render item id</param>
        void RemoveRenderItem(IdType id);
    }
}

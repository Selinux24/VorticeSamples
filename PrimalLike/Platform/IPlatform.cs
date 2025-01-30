using PrimalLike.EngineAPI;
using System;

namespace PrimalLike.Platform
{
    /// <summary>
    /// The base class for platform implementations.
    /// </summary>
    public interface IPlatform
    {
        /// <summary>
        /// Creates a new window.
        /// </summary>
        /// <param name="info">Initialization info</param>
        /// <param name="setDefault">Sets the new window as the default window</param>
        /// <returns>Returns the created window</returns>
        Window CreateWindow(IPlatformWindowInfo info, bool setDefault = true);
        /// <summary>
        /// Removes a window.
        /// </summary>
        /// <param name="id">Window id</param>
        void RemoveWindow(WindowId id);

        /// <summary>
        /// Gets the window handle.
        /// </summary>
        /// <param name="id">Window id</param>
        IntPtr GetWindowHandle(WindowId id);
        /// <summary>
        /// Toggle fullscreen.
        /// </summary>
        /// <param name="id">Window id</param>
        /// <param name="isFullscreen">Is fullscreen</param>
        void SetFullscreen(WindowId id, bool isFullscreen);
        /// <summary>
        /// Gets if the window is fullscreen.
        /// </summary>
        /// <param name="id">Window id</param>
        bool IsWindoFullscreen(WindowId id);
        /// <summary>
        /// Sets the caption of the window.
        /// </summary>
        /// <param name="id">Window id</param>
        /// <param name="caption">Caption</param>
        void SetCaption(WindowId id, string caption);
        /// <summary>
        /// Resizes the window.
        /// </summary>
        /// <param name="id">Window id</param>
        /// <param name="width">Width</param>
        /// <param name="height">Height</param>
        void Resize(WindowId id, uint width, uint height);
        /// <summary>
        /// Gets the window width.
        /// </summary>
        /// <param name="id">Window id</param>
        uint GetWindowWidth(WindowId id);
        /// <summary>
        /// Gets the window height.
        /// </summary>
        /// <param name="id">Window id</param>
        uint GetWindowHeight(WindowId id);
        /// <summary>
        /// Gets if the window is closed.
        /// </summary>
        /// <param name="id">Window id</param>
        bool IsWindowClosed(WindowId id);

        /// <summary>
        /// Runs the platform.
        /// </summary>
        void Run();
    }
}

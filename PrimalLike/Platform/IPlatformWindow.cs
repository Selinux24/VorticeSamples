using System;

namespace PrimalLike.Platform
{
    public interface IPlatformWindow
    {
        /// <summary>
        /// Gets the id of the window.
        /// </summary>
        WindowId Id { get; set; }
        /// <summary>
        /// Gets the handle of the window.
        /// </summary>
        IntPtr Handle { get; }
        /// <summary>
        /// Gets whether the window is in full screen mode.
        /// </summary>
        bool IsFullscreen { get; }
        /// <summary>
        /// Gets the width of the window.
        /// </summary>
        uint Width { get; }
        /// <summary>
        /// Gets the height of the window.
        /// </summary>
        uint Height { get; }
        /// <summary>
        /// Gets whether the window is closed.
        /// </summary>
        bool IsClosed { get; }

        /// <summary>
        /// Sets the full screen state of the window.
        /// </summary>
        /// <param name="fullScreen">Full screen</param>
        void SetFullscreen(bool fullScreen);
        /// <summary>
        /// Sets the caption of the window.
        /// </summary>
        /// <param name="caption">Caption</param>
        void SetCaption(string caption);
        /// <summary>
        /// Resizes the window.
        /// </summary>
        /// <param name="width">Width</param>    
        /// <param name="height">Height</param>
        void Resize(uint width, uint height);
    }
}

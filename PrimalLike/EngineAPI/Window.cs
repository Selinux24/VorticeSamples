global using WindowId = System.UInt32;
using PrimalLike.Common;
using PrimalLike.Platform;
using System;
using System.Diagnostics;

namespace PrimalLike.EngineAPI
{
    /// <summary>
    /// Represents a window.
    /// </summary>
    public class Window
    {
        /// <summary>
        /// The window id.
        /// </summary>
        WindowId id = WindowId.MaxValue;

        /// <summary>
        /// Constructor.
        /// </summary>
        public Window()
        {

        }
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="id">Window id</param>
        public Window(WindowId id)
        {
            this.id = id;
        }

        /// <summary>
        /// Gets the window id.
        /// </summary>
        public WindowId Id { get => id; internal set => id = value; }
        /// <summary>
        /// Gets if the window is valid.
        /// </summary>
        public bool IsValid { get => IdDetail.IsValid(id); }

        /// <summary>
        /// Toggles fullscreen.
        /// </summary>
        public bool IsFullscreen { get => PlatformBase.IsWindoFullscreen(id); set => SetFullscreen(value); }
        /// <summary>
        /// Gets the window handle.
        /// </summary>
        public IntPtr Handle { get => PlatformBase.GetWindowHandle(id); }
        /// <summary>
        /// Gets the window width.
        /// </summary>
        public uint Width { get => PlatformBase.GetWindowWidth(id); }
        /// <summary>
        /// Gets the window height.
        /// </summary>
        public uint Height { get => PlatformBase.GetWindowHeight(id); }
        /// <summary>
        /// Gets if the window is closed.
        /// </summary>
        public bool IsClosed { get => PlatformBase.IsWindowClosed(id); }

        /// <summary>
        /// Sets the window fullscreen.
        /// </summary>
        /// <param name="isFullscreen">Is fullscreen</param>
        public void SetFullscreen(bool isFullscreen)
        {
            Debug.Assert(IsValid);
            PlatformBase.SetFullscreen(id, isFullscreen);
        }
        /// <summary>
        /// Sets the window caption.
        /// </summary>
        /// <param name="caption">Caption</param>
        public void SetCaption(string caption)
        {
            Debug.Assert(IsValid);
            PlatformBase.SetCaption(id, caption);
        }
        /// <summary>
        /// Resizes the window.
        /// </summary>
        /// <param name="width">Width</param>
        /// <param name="height">Height</param>
        public void Resize(uint width, uint height)
        {
            Debug.Assert(IsValid);
            PlatformBase.Resize(id, width, height);
        }
    }
}

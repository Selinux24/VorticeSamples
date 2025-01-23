
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


        nint GetWindowHandle(WindowId id);
        void SetFullscreen(WindowId id, bool isFullscreen);
        bool IsWindoFullscreen(WindowId id);
        void SetCaption(WindowId id, string caption);
        void Resize(WindowId id, uint width, uint height);
        uint GetWindowWidth(WindowId id);
        uint GetWindowHeight(WindowId id);
        bool IsWindowClosed(WindowId id);

        /// <summary>
        /// Runs the platform.
        /// </summary>
        void Run();
    }
}

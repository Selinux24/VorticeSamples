
namespace PrimalLike.Platform
{
    /// <summary>
    /// The base class for platform implementations.
    /// </summary>
    public interface IPlatform
    {
        /// <summary>
        /// Gets the main window.
        /// </summary>
        PlatformWindow MainWindow { get; }

        /// <summary>
        /// Creates a new window.
        /// </summary>
        /// <param name="info">Initialization info</param>
        /// <param name="setDefault">Sets the new window as the default window</param>
        /// <returns>Returns the created window</returns>
        PlatformWindow CreateWindow(IPlatformWindowInfo info, bool setDefault = true);
        /// <summary>
        /// Removes a window.
        /// </summary>
        /// <param name="window">Window to remove</param>
        void RemoveWindow(PlatformWindow window);
        /// <summary>
        /// Runs the platform.
        /// </summary>
        void Run();
    }
}

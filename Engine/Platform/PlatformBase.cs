
namespace Engine.Platform
{
    /// <summary>
    /// The base class for platform implementations.
    /// </summary>
    public abstract class PlatformBase()
    {
        /// <summary>
        /// Gets the main window.
        /// </summary>
        public abstract PlatformWindow MainWindow { get; }

        /// <summary>
        /// Creates a new window.
        /// </summary>
        /// <param name="info">Initialization info</param>
        /// <param name="setDefault">Sets the new window as the default window</param>
        /// <returns>Returns the created window</returns>
        public abstract PlatformWindow CreateWindow(IPlatformWindowInfo info, bool setDefault = true);
        /// <summary>
        /// Removes a window.
        /// </summary>
        /// <param name="window">Window to remove</param>
        public abstract void RemoveWindow(PlatformWindow window);
        /// <summary>
        /// Runs the platform.
        /// </summary>
        public abstract void Run();
    }
}

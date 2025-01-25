
namespace PrimalLike.Platform
{
    /// <summary>
    /// Represents the initialization of a platform window.
    /// </summary>
    public interface IPlatformWindowInfo
    {
        /// <summary>
        /// Gets or sets the caption of the window.
        /// </summary>
        string Caption { get; set; }
        /// <summary>
        /// Gets or sets the client area of the window.
        /// </summary>
        ClientArea ClientArea { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the window is full screen.
        /// </summary>
        bool IsFullScreen { get; set; }
    }
}

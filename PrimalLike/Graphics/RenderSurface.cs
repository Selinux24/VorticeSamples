using PrimalLike.Platform;

namespace PrimalLike.Graphics
{
    /// <summary>
    /// Render surface structure.
    /// </summary>
    public struct RenderSurface
    {
        /// <summary>
        /// Window
        /// </summary>
        public PlatformWindow Window { get; set; }
        /// <summary>
        /// Surface
        /// </summary>
        public ISurface Surface { get; set; }
    }
}

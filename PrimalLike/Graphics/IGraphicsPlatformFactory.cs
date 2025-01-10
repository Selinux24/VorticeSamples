
namespace PrimalLike.Graphics
{
    /// <summary>
    /// Graphics platform factory interface.
    /// </summary>
    public interface IGraphicsPlatformFactory
    {
        /// <summary>
        /// Creates a graphics platform.
        /// </summary>
        IGraphicsPlatform CreateGraphicsPlatform();
    }
}

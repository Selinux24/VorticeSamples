using PrimalLike.Platform;

namespace WindowsPlatform
{
    /// <summary>
    /// Win32 platform factory.
    /// </summary>
    public sealed class Win32PlatformFactory : IPlatformFactory
    {
        /// <inheritdoc/>
        public IPlatform CreatePlatform()
        {
            return new Win32Platform();
        }
    }
}

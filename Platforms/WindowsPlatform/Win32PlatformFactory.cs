using PrimalLike.Platform;

namespace WindowsPlatform
{
    public sealed class Win32PlatformFactory : IPlatformFactory
    {
        public IPlatform CreatePlatform()
        {
            return new Win32Platform();
        }
    }
}

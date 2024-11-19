using Engine.Platform;

namespace WindowsPlatform
{
    public sealed class Win32PlatformFactory : IPlatformFactory
    {
        public PlatformBase CreatePlatform()
        {
            return new Win32Platform();
        }
    }
}

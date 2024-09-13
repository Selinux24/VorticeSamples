using Engine;

namespace WindowsPlatform
{
    public class Win32PlatformFactory : IPlatformFactory
    {
        public Platform CreatePlatform()
        {
            return new Win32Platform();
        }
    }
}

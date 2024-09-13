using WindowsPlatform;

namespace DX12Windows
{
    internal class Program
    {
        static void Main()
        {
            HelloWorldApp
                .Start<Win32PlatformFactory>()
                .Run();
        }
    }
}

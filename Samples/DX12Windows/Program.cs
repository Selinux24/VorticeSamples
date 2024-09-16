using Direct3D12;
using WindowsPlatform;

namespace DX12Windows
{
    internal class Program
    {
        static void Main()
        {
            HelloWorldApp
                .Start<Win32PlatformFactory, D3D12GraphicsFactory>()
                .Run();
        }
    }
}

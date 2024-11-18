using Direct3D12;
using Engine.Components;
using Engine.Platform;
using WindowsPlatform;

namespace DX12Windows
{
    internal class Program
    {
        static void Main()
        {
            PlatformWindowInfo windowInfo = new()
            {
                Title = "DX12 for Windows",
                ClientArea = new System.Drawing.Rectangle(0, 0, 1280, 720),
                IsFullScreen = false,
            };

            GameEntity.RegisterScript<TestScript>();

            HelloWorldApp
                .Start<Win32PlatformFactory, D3D12GraphicsFactory>(windowInfo)
                .Run();
        }
    }
}

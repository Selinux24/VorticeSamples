using Direct3D12;
using Engine.Components;
using WindowsPlatform;

namespace DX12Windows
{
    internal class Program
    {
        static void Main()
        {
            Win32WindowInfo windowInfo1 = new()
            {
                Title = "DX12 for Windows 1",
                ClientArea = new System.Drawing.Rectangle(0, 0, 400, 800),
                IsFullScreen = false,
            };
            Win32WindowInfo windowInfo2 = new()
            {
                Title = "DX12 for Windows 2",
                ClientArea = new System.Drawing.Rectangle(410, 0, 600, 800),
                IsFullScreen = false,
            };

            GameEntity.RegisterScript<TestScript>();

            var app = HelloWorldApp.Start<Win32PlatformFactory, D3D12GraphicsFactory>();
            app.CreateWindow(windowInfo1);
            app.CreateWindow(windowInfo2);
            app.Run();
        }
    }
}

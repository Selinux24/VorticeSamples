using Direct3D12;
using Engine.Components;
using Engine.Graphics;
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
                ClientArea = new System.Drawing.Rectangle(100, 100, 400, 800),
                IsFullScreen = false,
            };
            Win32WindowInfo windowInfo2 = new()
            {
                Title = "DX12 for Windows 2",
                ClientArea = new System.Drawing.Rectangle(150, 150, 800, 400),
                IsFullScreen = false,
            };
            Win32WindowInfo windowInfo3 = new()
            {
                Title = "DX12 for Windows 3",
                ClientArea = new System.Drawing.Rectangle(200, 200, 400, 400),
                IsFullScreen = false,
            };
            Win32WindowInfo windowInfo4 = new()
            {
                Title = "DX12 for Windows 4",
                ClientArea = new System.Drawing.Rectangle(250, 250, 800, 600),
                IsFullScreen = false,
            };

            GameEntity.RegisterScript<TestScript>();

            var app = HelloWorldApp.Start<Win32PlatformFactory, D3D12GraphicsFactory>();

            RenderSurface[] renderSurfaces = 
            [
                new RenderSurface { Window = app.CreateWindow(windowInfo1) },
                new RenderSurface { Window = app.CreateWindow(windowInfo2) },
                new RenderSurface { Window = app.CreateWindow(windowInfo3) },
                new RenderSurface { Window = app.CreateWindow(windowInfo4) },
            ];

            app.Run();

            foreach (var renderSurface in renderSurfaces)
            {
                app.RemoveWindow(renderSurface.Window);
            }
        }
    }
}

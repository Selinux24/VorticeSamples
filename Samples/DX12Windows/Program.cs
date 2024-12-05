using Direct3D12;
using Engine.Components;
using System;
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

            try
            {
                var app = HelloWorldApp.Start<Win32PlatformFactory, D3D12GraphicsFactory>();
                app.CreateWindow(windowInfo1);
                app.CreateWindow(windowInfo2);
                app.CreateWindow(windowInfo3);
                app.CreateWindow(windowInfo4);
                app.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error ocurred. Enter to continue.");
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }
        }
    }
}

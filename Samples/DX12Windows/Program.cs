using Direct3D12;
using DX12Windows.Content;
using DX12Windows.Lights;
using DX12Windows.Shaders;
using PrimalLike;
using PrimalLike.EngineAPI;
using System;
using WindowsPlatform;

namespace DX12Windows
{
    class Program
    {
        private const string assetsFolder = "./Assets";
        private const string outputsFolder = "./Content";

        private static HelloWorldApp app;
        private static HelloWorldComponent renderComponent;

        private static ITestRenderItem renderItem;

        static void Main()
        {
            while (ChooseScene() == -1) { }

            EngineShadersHelper.Compile();

            InitializeApp();

            renderItem.Load(assetsFolder, outputsFolder);

            CreateWindow();

            LightGenerator.GenerateLights();

            InitializeInput();

            app.Run();
        }
        static int ChooseScene()
        {
            Console.Clear();
            Console.WriteLine("Choose the scene: ");
            Console.WriteLine("1. Model");
            Console.WriteLine("2. LabScene");
            Console.WriteLine("3. ToyTank");
            Console.WriteLine("4. Humvee");
            Console.WriteLine("5. M-24");
            Console.WriteLine("6. Exit");
            var key = Console.ReadKey();
            if (key.KeyChar == '1')
            {
                renderItem = new ModelRenderItem();
            }
            else if (key.KeyChar == '2')
            {
                renderItem = new LabSceneRenderItem();
            }
            else if (key.KeyChar == '3')
            {
                renderItem = new ToyTankRenderItem();
            }
            else if (key.KeyChar == '4')
            {
                renderItem = new HumveeRenderItem();
            }
            else if (key.KeyChar == '5')
            {
                renderItem = new M24RenderItem();
            }
            else if (key.KeyChar == '6')
            {
                return 0;
            }
            else
            {
                return -1;
            }

            return 1;
        }

        static void InitializeApp()
        {
            app = HelloWorldApp.Start<Win32PlatformFactory, D3D12GraphicsPlatformFactory>();
            app.OnShutdown += AppShutdown;
        }
        static void CreateWindow()
        {
            Win32WindowInfo windowInfo = new()
            {
                Caption = "DX12 for Windows",
                ClientArea = new(50, 50, 800, 600),
                IsFullScreen = false,
                WndProc = CustomWndProc,
            };
            renderComponent = Application.CreateRenderComponent<HelloWorldComponent>(windowInfo);

            Entity entity = HelloWorldApp.CreateOneGameEntity<Scripts.CameraScript>(renderItem.InitialCameraPosition, renderItem.InitialCameraRotation);
            renderComponent.CreateCamera(entity);
            renderComponent.UpdateFrameInfo(renderItem.GetRenderItems(), [10f]);
        }
        static IntPtr CustomWndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            const uint WM_CAPTURECHANGED = 0x0215;

            switch (msg)
            {
                case WM_CAPTURECHANGED:
                    renderComponent.Resized();
                    return 0;
                default:
                    break;
            }

            return Win32Window.DefaultWndProc(hwnd, msg, wParam, lParam);
        }

        static void AppShutdown(object sender, EventArgs e)
        {
            renderItem.DestroyRenderItems();

            Application.RemoveRenderComponent(renderComponent);

            LightGenerator.RemoveLights();

            Input.UnBind("move");
        }

        static void InitializeInput()
        {
            InputSource source = new("move");
            source.SourceType = InputSources.Keyboard;

            source.Code = (uint)InputCodes.KeyA;
            source.Multiplier = 1f;
            source.Axis = InputAxis.X;
            Input.Bind(source);

            source.Code = (uint)InputCodes.KeyD;
            source.Multiplier = -1f;
            Input.Bind(source);

            source.Code = (uint)InputCodes.KeyW;
            source.Multiplier = 1f;
            source.Axis = InputAxis.Z;
            Input.Bind(source);

            source.Code = (uint)InputCodes.KeyS;
            source.Multiplier = -1f;
            Input.Bind(source);

            source.Code = (uint)InputCodes.KeyQ;
            source.Multiplier = -1f;
            source.Axis = InputAxis.Y;
            Input.Bind(source);

            source.Code = (uint)InputCodes.KeyE;
            source.Multiplier = 1f;
            Input.Bind(source);
        }
    }
}

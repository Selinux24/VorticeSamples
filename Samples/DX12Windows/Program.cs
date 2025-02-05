using Direct3D12;
using DX12Windows.Content;
using DX12Windows.Shaders;
using PrimalLike;
using PrimalLike.Components;
using PrimalLike.EngineAPI;
using System;
using System.Collections.Generic;
using System.Numerics;
using Vortice.Mathematics;
using WindowsPlatform;

namespace DX12Windows
{
    internal class Program
    {
        private const string assetsFolder = "./Assets";
        private const string outputsFolder = "./Content";

        private const uint WM_CAPTURECHANGED = 0x0215;

        private static HelloWorldApp app;
        private static HelloWorldComponent component;

        private static readonly ulong leftSet = 0;
        private static readonly ulong rightSet = 1;
        private static readonly List<Light> lights = [];

        private static readonly LabSceneRenderItem renderItem = new();

        static void Main()
        {
            EngineShadersHelper.Compile();

            InitializeApp();

            renderItem.Load(assetsFolder, outputsFolder);

            CreateWindow();

            GenerateLights();

            app.Run();
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
            component = Application.CreateRenderComponent<HelloWorldComponent>(windowInfo);

            EntityInfo entityInfo = new()
            {
                Transform = new()
                {
                    Rotation = renderItem.InitialCameraRotation,
                    Position = renderItem.InitialCameraPosition,
                },
            };
            component.CreateCamera(entityInfo);
            component.UpdateFrameInfo(renderItem.GetRenderItems(), [10f]);
        }
        static IntPtr CustomWndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WM_CAPTURECHANGED:
                    component.Resized();
                    return 0;
                default:
                    break;
            }

            return Win32Window.DefaultWndProc(hwnd, msg, wParam, lParam);
        }

        static Vector3 RGBToColor(byte r, byte g, byte b)
        {
            return new()
            {
                X = r / 255f,
                Y = g / 255f,
                Z = b / 255f
            };
        }
        static void GenerateLights()
        {
            // LEFT_SET
            LightInitInfo info = new()
            {
                EntityId = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, Vector3.Zero, null).Id,
                LightType = LightTypes.Directional,
                LightSetKey = leftSet,
                Intensity = 1f,
                Color = RGBToColor(174, 174, 174)
            };
            lights.Add(Application.CreateLight(info));

            info.EntityId = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, new(MathHelper.PiOver2, 0, 0), null).Id;
            info.Color = RGBToColor(17, 27, 48);
            lights.Add(Application.CreateLight(info));

            info.EntityId = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, new(-MathHelper.PiOver2, 0, 0), null).Id;
            info.Color = RGBToColor(63, 47, 30);
            lights.Add(Application.CreateLight(info));

            // RIGHT_SET
            info.EntityId = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, Vector3.Zero, null).Id;
            info.LightSetKey = rightSet;
            info.Color = RGBToColor(150, 100, 200);
            lights.Add(Application.CreateLight(info));

            info.EntityId = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, new(MathHelper.PiOver2, 0, 0), null).Id;
            info.Color = RGBToColor(17, 27, 48);
            lights.Add(Application.CreateLight(info));

            info.EntityId = HelloWorldApp.CreateOneGameEntity(Vector3.Zero, new(-MathHelper.PiOver2, 0, 0), null).Id;
            info.Color = RGBToColor(63, 47, 30);
            lights.Add(Application.CreateLight(info));
        }
        static void RemoveLights()
        {
            foreach (var light in lights)
            {
                uint id = light.EntityId;
                Application.RemoveLight(light.Id, light.LightSetKey);
                RemoveGameEntity(id);
            }

            lights.Clear();
        }
        static void RemoveGameEntity(uint id)
        {
            Application.RemoveEntity(id);
        }

        static void AppShutdown(object sender, EventArgs e)
        {
            renderItem.DestroyRenderItems();

            Application.RemoveRenderComponent(component);

            RemoveLights();
        }
    }
}

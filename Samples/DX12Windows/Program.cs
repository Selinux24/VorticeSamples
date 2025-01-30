using Direct3D12;
using Direct3D12.Shaders;
using PrimalLike;
using PrimalLike.Common;
using PrimalLike.Components;
using PrimalLike.Content;
using PrimalLike.EngineAPI;
using ShaderCompiler;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using Vortice.Mathematics;
using WindowsPlatform;

namespace DX12Windows
{
    internal class Program
    {
        private const string shadersSourceDir = "../../../../../Libs/Direct3D12/Shaders/";
        private const string shadersIncludeDir = "../../../../../Libs/Direct3D12/Shaders/";
        private const string shadersOutputPath = "./Content/engineShaders.bin";
        private const string testModelFile = "./Content/Model.model";
        private const uint WM_CAPTURECHANGED = 0x0215;

        private static readonly EngineShaderInfo[] engineShaderFiles =
        [
            new ((int)EngineShaders.FullScreenTriangleVs, new (Path.Combine(shadersSourceDir, "FullScreenTriangle.hlsl"), "FullScreenTriangleVS", ShaderStage.Vertex)),
            new ((int)EngineShaders.FillColorPs, new (Path.Combine(shadersSourceDir, "FillColor.hlsl"), "FillColorPS", ShaderStage.Pixel)),
            new ((int)EngineShaders.PostProcessPs, new (Path.Combine(shadersSourceDir, "PostProcess.hlsl"), "PostProcessPS", ShaderStage.Pixel)),
        ];

        private static HelloWorldApp app;
        private static HelloWorldComponent component;
        private static uint modelId;
        private static uint itemId;

        private static readonly ulong leftSet = 0;
        private static readonly ulong rightSet = 1;
        private static readonly List<Light> lights = [];

        static void Main()
        {
            InitializeApp();

            LoadTestModel();
            CreateRenderItem();
            CreateWindow();

            GenerateLights();

            app.Run();
        }

        static void InitializeApp()
        {
            if (!Application.RegisterScript<HelloWorldScript>())
            {
                Console.WriteLine("Failed to register TestScript");
            }

            if (!Compiler.CompileShaders(engineShaderFiles, shadersIncludeDir, shadersOutputPath))
            {
                Console.WriteLine("Engine shaders compilation failed");
            }

            app = HelloWorldApp.Start<Win32PlatformFactory, D3D12GraphicsPlatformFactory>();
            app.OnShutdown += AppShutdown;
        }
        static void LoadTestModel()
        {
            using var file = new MemoryStream(File.ReadAllBytes(testModelFile));
            modelId = ContentToEngine.CreateResource(file, AssetTypes.Mesh);
        }
        static Entity CreateOneGameEntity(Vector3 position, Vector3 rotation, string scriptName)
        {
            TransformInfo transform = new()
            {
                Position = position,
                Rotation = Quaternion.CreateFromYawPitchRoll(rotation.X, rotation.Y, rotation.Z)
            };

            ScriptInfo script = new();
            if (!string.IsNullOrEmpty(scriptName))
            {
                script.ScriptCreator = Script.GetScriptCreator(IdDetail.StringHash(scriptName));
            }

            EntityInfo entityInfo = new()
            {
                Transform = transform,
                Script = script,
            };

            Entity ntt = Application.CreateEntity(entityInfo);
            Debug.Assert(ntt.IsValid);
            return ntt;
        }
        static void CreateRenderItem()
        {
            itemId = HelloWorldRenderItem.CreateRenderItem(CreateOneGameEntity(Vector3.Zero, Vector3.Zero, nameof(HelloWorldScript)).Id);
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
            component.UpdateFrameInfo([itemId], [10f]);
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
                EntityId = CreateOneGameEntity(Vector3.Zero, Vector3.Zero, null).Id,
                LightType = LightTypes.Directional,
                LightSetKey = leftSet,
                Intensity = 1f,
                Color = RGBToColor(174, 174, 174)
            };
            lights.Add(Application.CreateLight(info));

            info.EntityId = CreateOneGameEntity(Vector3.Zero, new(MathHelper.PiOver2, 0, 0), null).Id;
            info.Color = RGBToColor(17, 27, 48);
            lights.Add(Application.CreateLight(info));

            info.EntityId = CreateOneGameEntity(Vector3.Zero, new(-MathHelper.PiOver2, 0, 0), null).Id;
            info.Color = RGBToColor(63, 47, 30);
            lights.Add(Application.CreateLight(info));

            // RIGHT_SET
            info.EntityId = CreateOneGameEntity(Vector3.Zero, Vector3.Zero, null).Id;
            info.LightSetKey = rightSet;
            info.Color = RGBToColor(150, 100, 200);
            lights.Add(Application.CreateLight(info));

            info.EntityId = CreateOneGameEntity(Vector3.Zero, new(MathHelper.PiOver2, 0, 0), null).Id;
            info.Color = RGBToColor(17, 27, 48);
            lights.Add(Application.CreateLight(info));

            info.EntityId = CreateOneGameEntity(Vector3.Zero, new(-MathHelper.PiOver2, 0, 0), null).Id;
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
            HelloWorldRenderItem.DestroyRenderItem(itemId);

            if (IdDetail.IsValid(modelId))
            {
                ContentToEngine.DestroyResource(modelId, AssetTypes.Mesh);
            }

            Application.RemoveRenderComponent(component);

            RemoveLights();
        }
    }
}

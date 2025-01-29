using Direct3D12;
using Direct3D12.Content;
using Direct3D12.Shaders;
using PrimalLike;
using PrimalLike.Common;
using PrimalLike.Components;
using PrimalLike.Content;
using PrimalLike.EngineAPI;
using ShaderCompiler;
using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using Vortice.Direct3D12;
using WindowsPlatform;

namespace DX12Windows
{
    internal class Program
    {
        private const string shadersSourceDir = "../../../../../Libs/Direct3D12/Shaders/";
        private const string shadersIncludeDir = "../../../../../Libs/Direct3D12/Shaders/";
        private const string shadersOutputPath = "./Content/engineShaders.bin";
        private const string testModelFile = "./Content/Model.model";

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

        static void Main()
        {
            InitializeApp();

            LoadTestModel();
            CreateRenderItem();
            CreateWindow();

            app.Run();
        }

        static void InitializeApp()
        {
            if (!Application.RegisterScript<TestScript>())
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
        static Entity CreateOneGameEntity(Vector3 position, Vector3 rotation, bool rotates)
        {
            TransformInfo transform = new()
            {
                Position = position,
                Rotation = Quaternion.CreateFromYawPitchRoll(rotation.X, rotation.Y, rotation.Z)
            };

            ScriptInfo script = new();
            if (rotates)
            {
                script.ScriptCreator = (entity) => new TestScript(entity);
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
            itemId = HelloWorldRenderItem.CreateRenderItem(CreateOneGameEntity(Vector3.Zero, Vector3.Zero, true).Id);
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
        static void AppShutdown(object sender, EventArgs e)
        {
            HelloWorldRenderItem.DestroyRenderItem(itemId);

            if (IdDetail.IsValid(modelId))
            {
                ContentToEngine.DestroyResource(modelId, AssetTypes.Mesh);
            }

            Application.RemoveRenderComponent(component);
        }

        const uint WM_CAPTURECHANGED = 0x0215;
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
    }
}
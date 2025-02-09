using Direct3D12;
using Direct3D12.Shaders;
using NUnit.Framework;
using PrimalLike;
using PrimalLike.Common;
using PrimalLike.Components;
using PrimalLike.Content;
using PrimalLike.EngineAPI;
using PrimalLike.Graphics;
using PrimalLike.Platform;
using ShaderCompiler;
using System;
using System.IO;
using System.Numerics;
using System.Threading;
using WindowsPlatform;

namespace D3D12LibTests
{
    public class RendererTest
    {
        class TestApp(IPlatformFactory platformFactory, IGraphicsPlatformFactory graphicsFactory)
            : Application("Content/Game.bin", platformFactory, graphicsFactory)
        {
            public static TestApp Start<TPlatform, TGraphics>()
                where TPlatform : IPlatformFactory, new()
                where TGraphics : IGraphicsPlatformFactory, new()
            {
                return new TestApp(new TPlatform(), new TGraphics());
            }
        }

        class TestScript : EntityScript
        {
            public TestScript() : base()
            {
            }
            public TestScript(Entity entity) : base(entity)
            {
            }

            public override void Update(float deltaTime)
            {
            }
        }

        class CameraSurface : RenderComponent
        {
            private FrameInfo frameInfo = new();

            public Camera Camera { get; set; }
            public Entity Entity { get; set; }

            public CameraSurface(IPlatformWindowInfo info) : base(info)
            {
                Surface = Application.CreateRenderSurface(info);
            }

            public override void CreateCamera(Entity entity)
            {
                Entity = entity;
                Camera = Application.CreateCamera(new PerspectiveCameraInitInfo(Entity.Id));
                Camera.AspectRatio = (float)Surface.Window.Width / Surface.Window.Height;
            }

            public void UpdateFrameInfo(uint[] items, float[] thresholds)
            {
                frameInfo.CameraId = Camera.Id;
                frameInfo.RenderItemIds = items;
                frameInfo.RenderItemCount = (uint)items.Length;
                frameInfo.Thresholds = thresholds;
                frameInfo.LightSetKey = ulong.MaxValue;
            }

            public override FrameInfo GetFrameInfo()
            {
                return frameInfo;
            }
            public override void Resized()
            {
                Surface.Surface.Resize(Surface.Window.Width, Surface.Window.Height);
                Camera.AspectRatio = (float)Surface.Window.Width / Surface.Window.Height;
            }
            public override void Remove()
            {
                Application.RemoveRenderSurface(Surface);
                Application.RemoveCamera(Camera.Id);
                Application.RemoveEntity(Entity.Id);
            }
        }

        private const uint WM_CAPTURECHANGED = 0x0215;
        private static IntPtr CustomWndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WM_CAPTURECHANGED:
                    Array.Find(cameraSurfaces, c => c.Surface.Window.Handle == hwnd)?.Resized();
                    return 0;
                default:
                    break;
            }

            return Win32Window.DefaultWndProc(hwnd, msg, wParam, lParam);
        }

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

        private TestApp app;
        private static CameraSurface[] cameraSurfaces;

        private uint itemId = IdDetail.InvalidId;
        private uint modelId = IdDetail.InvalidId;

        private const int numThreads = 8;
        private readonly Thread[] workers = new Thread[numThreads];
        private readonly byte[] buffer = new byte[1024 * 1024];

        // Test preparation
        [OneTimeSetUp]
        public void Setup()
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = 0;
            }
        }

        private void InitializeApplication()
        {
            var resCompile = Compiler.CompileShaders(engineShaderFiles, shadersIncludeDir, shadersOutputPath);
            Assert.That(resCompile, "Shader compilation error.");

            app = TestApp.Start<Win32PlatformFactory, D3D12GraphicsPlatformFactory>();
            Assert.That(app != null, "Application start error.");

            app.OnShutdown += AppShutdown;
        }

        private void InitTestWorkers()
        {
            //Initalize worker threads
            for (int i = 0; i < numThreads; i++)
            {
                workers[i] = new Thread(BufferWorker);
            }

            // Start worker threads
            for (int i = 0; i < numThreads; i++)
            {
                workers[i].Start();
            }
        }
        private void JoinTestWorkers()
        {
            for (int i = 0; i < numThreads; i++)
            {
                workers[i].Join();
            }
        }
        private void BufferWorker()
        {
            while (!app.IsExiting)
            {
                var resource = D3D12Helpers.CreateBuffer(buffer, (uint)buffer.Length);
                D3D12Helpers.DeferredRelease(resource);
            }
        }

        private void LoadTestModel()
        {
            using var file = new MemoryStream(File.ReadAllBytes(testModelFile));
            modelId = ContentToEngine.CreateResource(file, AssetTypes.Mesh);
            Assert.That(modelId != uint.MaxValue, "Model creation error.");
        }

        private void CreateCameras()
        {
            Win32WindowInfo[] initInfos =
            [
                new()
                {
                    Caption = "DX12 for Windows 1",
                    ClientArea = new(100, 100, 400, 800),
                    IsFullScreen = false,
                    WndProc = CustomWndProc,
                },
                new()
                {
                    Caption = "DX12 for Windows 2",
                    ClientArea = new(150, 150, 800, 400),
                    IsFullScreen = false,
                    WndProc = CustomWndProc,
                },
                new()
                {
                    Caption = "DX12 for Windows 3",
                    ClientArea = new(200, 200, 400, 400),
                    IsFullScreen = false,
                    WndProc = CustomWndProc,
                },
                new()
                {
                    Caption = "DX12 for Windows 4",
                    ClientArea = new(250, 250, 800, 600),
                    IsFullScreen = false,
                    WndProc = CustomWndProc,
                }
            ];

            cameraSurfaces = new CameraSurface[initInfos.Length];

            for (uint i = 0; i < initInfos.Length; i++)
            {
                cameraSurfaces[i] = Application.CreateRenderComponent<CameraSurface>(initInfos[i]);

                EntityInfo entityInfo = new()
                {
                    Transform = new()
                    {
                        Rotation = Quaternion.CreateFromYawPitchRoll(0, 3.14f, 0),
                        Position = new(0, 1f, 3f),
                    },
                };
                Entity entity = Application.CreateEntity(entityInfo);
                cameraSurfaces[i].CreateCamera(entity);
                cameraSurfaces[i].UpdateFrameInfo([itemId], [10f]);
            }
        }
        private void CreateRenderItem()
        {
            itemId = RenderItem.CreateRenderItem(Application.CreateEntity(new()).Id);
        }

        [Test()]
        public void RenderTest()
        {
            InitializeApplication();

            LoadTestModel();
            CreateRenderItem();
            CreateCameras();

            app.Run();

            Assert.That(true);
        }
        [Test()]
        public void UploadContextTest()
        {
            InitializeApplication();

            LoadTestModel();
            CreateRenderItem();
            CreateCameras();

            // Congifure worker threads
            InitTestWorkers();

            app.Run();

            // Shutdown worker threads
            JoinTestWorkers();

            Assert.That(true);
        }

        private void AppShutdown(object sender, EventArgs e)
        {
            RenderItem.DestroyRenderItem(itemId);

            if (IdDetail.IsValid(modelId))
            {
                ContentToEngine.DestroyResource(modelId, AssetTypes.Mesh);
            }

            for (int i = 0; i < cameraSurfaces.Length; i++)
            {
                Application.RemoveRenderComponent(cameraSurfaces[i]);
            }
        }
    }
}
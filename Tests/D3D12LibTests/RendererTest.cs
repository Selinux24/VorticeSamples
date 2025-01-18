using Direct3D12;
using NUnit.Framework;
using PrimalLike;
using PrimalLike.Common;
using PrimalLike.Components;
using PrimalLike.Content;
using PrimalLike.EngineAPI;
using PrimalLike.Graphics;
using PrimalLike.Platform;
using ShaderCompiler;
using System.Diagnostics;
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

        private const string shadersSourceDir = "../../../../../Libs/Direct3D12/Shaders/";
        private const string shadersOutputPath = "./Content/engineShaders.bin";
        private const string testModelFile = "./Content/Model.model";

        private static readonly EngineShaderInfo[] engineShaderFiles =
        [
            new ((int)EngineShaders.FullScreenTriangleVs, new ("FullScreenTriangle.hlsl", "FullScreenTriangleVS", ShaderStage.Vertex)),
            new ((int)EngineShaders.FillColorPs, new ("FillColor.hlsl", "FillColorPS", ShaderStage.Pixel)),
            new ((int)EngineShaders.PostProcessPs, new ("PostProcess.hlsl", "PostProcessPS", ShaderStage.Pixel)),
        ];

        private TestApp app;

        private Entity entity;
        private Camera camera;

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
            var resCompile = Compiler.CompileShaders(engineShaderFiles, shadersSourceDir, shadersOutputPath);
            Assert.That(resCompile, "Shader compilation error.");

            //bool resRegister = GameEntity.RegisterScript<TestScript>();
            //Assert.That(resRegister, "Test script registration error.");

            app = TestApp.Start<Win32PlatformFactory, D3D12GraphicsPlatformFactory>();
            Assert.That(app != null, "Application start error.");

            app.OnShutdown += AppShutdown;
        }
        private void ShowTestWindows()
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

            app.CreateWindow(windowInfo1);
            app.CreateWindow(windowInfo2);
            app.CreateWindow(windowInfo3);
            app.CreateWindow(windowInfo4);
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

        private static Entity CreateOneGameEntity()
        {
            EntityInfo entityInfo = new()
            {
                Transform = new()
                {
                    Rotation = Quaternion.CreateFromYawPitchRoll(0, 3.14f, 0)
                }
            };

            Entity ntt = GameEntity.Create(entityInfo);
            Debug.Assert(ntt.IsValid());
            return ntt;
        }
        private void LoadTestModel()
        {
            using var file = new MemoryStream(File.ReadAllBytes(testModelFile));
            modelId = ContentToEngine.CreateResource(file, AssetTypes.Mesh);
            Assert.That(modelId != uint.MaxValue, "Model creation error.");
        }
        private void CreateCamera()
        {
            entity = CreateOneGameEntity();
            camera = app.CreateCamera(new PerspectiveCameraInitInfo(entity.Id));
            Assert.That(camera.IsValid);
        }
        private void CreateRenderItem()
        {
            itemId = RenderItem.CreateRenderItem(CreateOneGameEntity().Id);
        }

        [Test()]
        public void RenderTest()
        {
            InitializeApplication();

            ShowTestWindows();

            LoadTestModel();
            CreateCamera();
            CreateRenderItem();

            app.Run();

            Assert.That(true);
        }
        [Test()]
        public void UploadModelTest()
        {
            InitializeApplication();

            ShowTestWindows();

            LoadTestModel();

            app.Run();

            Assert.That(true);
        }
        [Test()]
        public void UploadContextTest()
        {
            InitializeApplication();

            // Congifure worker threads
            InitTestWorkers();

            ShowTestWindows();

            app.Run();

            // Shutdown worker threads
            JoinTestWorkers();

            Assert.That(true);
        }
        [Test()]
        public void CameraTest()
        {
            InitializeApplication();

            ShowTestWindows();

            CreateCamera();

            app.Run();

            Assert.That(true);
        }

        private void AppShutdown(object sender, System.EventArgs e)
        {
            RenderItem.DestroyRenderItem(itemId);

            if (IdDetail.IsValid(modelId))
            {
                ContentToEngine.DestroyResource(modelId, AssetTypes.Mesh);
            }

            if (camera?.IsValid ?? false)
            {
                app.RemoveCamera(camera.Id);
            }
            if (entity?.IsValid() ?? false)
            {
                GameEntity.Remove(entity.Id);
            }
        }
    }
}
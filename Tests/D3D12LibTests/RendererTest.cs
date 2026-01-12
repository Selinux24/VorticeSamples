using Direct3D12;
using NUnit.Framework;
using PrimalLike;
using PrimalLike.Common;
using PrimalLike.Components;
using PrimalLike.Content;
using PrimalLike.EngineAPI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using WindowsPlatform;
using static Native32.User32;

namespace D3D12LibTests
{
    public class RendererTest
    {
        const string testModelFile = "./Content/Model.model";
        const string testTextureFile = "./Content/texture.texture";

        static readonly List<CameraSurface> cameraSurfaces = [];
        static bool resized = false;
        static IntPtr CustomWndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            bool toggleFullscreen = false;

            switch (msg)
            {
                case WindowMessages.WM_DESTROY:
                {
                    bool allClosed = true;
                    for (int i = 0; i < cameraSurfaces.Count; i++)
                    {
                        if (!cameraSurfaces[i].Surface.Window.IsValid)
                        {
                            continue;
                        }

                        if (!cameraSurfaces[i].Surface.Window.IsClosed)
                        {
                            allClosed = false;
                        }
                    }
                    if (allClosed)
                    {
                        _ = PostQuitMessage(0);
                        return 0;
                    }
                }
                break;
                case WindowMessages.WM_SIZE:
                {
                    resized = wParam != WM_SIZE_WPARAM.SIZE_MINIMIZED;
                    break;
                }
                case WindowMessages.WM_SYSCHAR:
                {
                    toggleFullscreen = wParam == VirtualKeys.VK_RETURN && (HIWORD(lParam) & KeystrokeFlags.KF_ALTDOWN) != 0;
                    break;
                }
                case WindowMessages.WM_KEYDOWN:
                {
                    if (wParam == VirtualKeys.VK_ESCAPE)
                    {
                        _ = PostMessageW(hwnd, WindowMessages.WM_CLOSE, 0, 0);
                        return 0;
                    }
                    break;
                }
            }

            if ((resized && GetKeyState((int)VirtualKeys.VK_LBUTTON) >= 0) || toggleFullscreen)
            {
                Window win = new((uint)GetWindowLongPtrW(hwnd, WindowLongIndex.GWL_USERDATA));
                for (int i = 0; i < cameraSurfaces.Count; i++)
                {
                    if (win.Id == cameraSurfaces[i].Surface.Window.Id)
                    {
                        if (toggleFullscreen)
                        {
                            win.SetFullscreen(!win.IsFullscreen);
                            // The default window procedure will play a system notification sound
                            // when pressing the Alt+Enter keyboard combination if WM_SYSCHAR is
                            // not handled. By returning 0 we can tell the system that we handled
                            // this message.
                            return 0;
                        }
                        else
                        {
                            cameraSurfaces[i].Surface.Surface.Resize(win.Width, win.Height);
                            cameraSurfaces[i].Camera.AspectRatio = (float)win.Width / win.Height;

                            resized = false;
                        }
                        break;
                    }
                }
            }

            return DefWindowProcW(hwnd, msg, wParam, lParam);
        }

        TestApp app;

        uint itemId = IdDetail.InvalidId;
        uint modelId = IdDetail.InvalidId;
        uint textureId = IdDetail.InvalidId;
        readonly ulong lightSetKey = 0;

        const int numThreads = 8;
        readonly Thread[] workers = new Thread[numThreads];
        const uint bufferLength = 1024 * 1024;
        readonly IntPtr buffer = Marshal.AllocHGlobal((int)bufferLength);

        // Test preparation
        [OneTimeSetUp]
        public void Setup()
        {
            IntPtr b = buffer;
            for (uint i = 0; i < bufferLength; i++)
            {
                Marshal.WriteByte(b, 0);
                b++;
            }
        }

        void InitializeApplication()
        {
            var resCompile = D3D12Graphics.CompileShaders();
            Assert.That(resCompile, "Shader compilation error.");

            app = TestApp.Start<Win32PlatformFactory, D3D12GraphicsPlatformFactory>();
            Assert.That(app != null, "Application start error.");

            app.OnShutdown += AppShutdown;
        }

        void InitTestWorkers()
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
        void JoinTestWorkers()
        {
            for (int i = 0; i < numThreads; i++)
            {
                workers[i].Join();
            }
        }
        void BufferWorker()
        {
            while (!app.IsExiting)
            {
                var resource = D3D12Helpers.CreateBuffer(buffer, bufferLength);
                D3D12Helpers.DeferredRelease(resource);

                //Output to the debug console to show activity
                Debug.WriteLine($"Worker {Environment.CurrentManagedThreadId} created and released a buffer.");
            }
        }

        void LoadTestModel()
        {
            using var file = new MemoryStream(File.ReadAllBytes(testModelFile));
            modelId = ContentToEngine.CreateResource(file, AssetTypes.Mesh);
            Assert.That(modelId != uint.MaxValue, "Model creation error.");
        }
        void LoadTestTexture()
        {
            using var file = new MemoryStream(File.ReadAllBytes(testTextureFile));
            textureId = ContentToEngine.CreateResource(file, AssetTypes.Texture);
            Assert.That(textureId != uint.MaxValue, "Texture creation error.");
        }

        void CreateRenderItem()
        {
            itemId = RenderItem.CreateRenderItem(Application.CreateEntity(new()).Id);
        }
        void CreateCameras()
        {
            Win32WindowInfo[] initInfos =
            [
                new()
                {
                    Caption = "DX12 for Windows 1",
                    ClientArea = new(100, 100, 400, 800),
                    IsFullScreen = false,
                    Callback = CustomWndProc,
                },
                new()
                {
                    Caption = "DX12 for Windows 2",
                    ClientArea = new(150, 150, 800, 400),
                    IsFullScreen = false,
                    Callback = CustomWndProc,
                },
                new()
                {
                    Caption = "DX12 for Windows 3",
                    ClientArea = new(200, 200, 400, 400),
                    IsFullScreen = false,
                    Callback = CustomWndProc,
                },
                new()
                {
                    Caption = "DX12 for Windows 4",
                    ClientArea = new(250, 250, 800, 600),
                    IsFullScreen = false,
                    Callback = CustomWndProc,
                }
            ];

            for (int i = 0; i < initInfos.Length; i++)
            {
                cameraSurfaces.Add(Application.CreateRenderComponent<CameraSurface>(initInfos[i]));

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
                cameraSurfaces[i].UpdateFrameInfo([itemId], [10f], lightSetKey);
            }
        }
        void CreateLights()
        {
            Application.CreateLightSet(lightSetKey);
        }

        [Test()]
        public void RenderTest()
        {
            InitializeApplication();

            LoadTestModel();
            LoadTestTexture();
            CreateRenderItem();
            CreateCameras();
            CreateLights();

            app.Run();

            Assert.That(true);
        }
        [Test()]
        public void UploadContextTest()
        {
            InitializeApplication();

            LoadTestModel();
            LoadTestTexture();
            CreateRenderItem();
            CreateCameras();
            CreateLights();

            // Congifure worker threads
            InitTestWorkers();

            app.Run();

            // Shutdown worker threads
            JoinTestWorkers();

            Assert.That(true);
        }

        void AppShutdown(object sender, EventArgs e)
        {
            RenderItem.DestroyRenderItem(itemId);

            if (IdDetail.IsValid(modelId))
            {
                ContentToEngine.DestroyResource(modelId, AssetTypes.Mesh);
            }

            if (IdDetail.IsValid(textureId))
            {
                ContentToEngine.DestroyResource(textureId, AssetTypes.Texture);
            }

            for (int i = 0; i < cameraSurfaces.Count; i++)
            {
                Application.RemoveRenderComponent(cameraSurfaces[i]);
            }

            Application.RemoveLightSet(lightSetKey);
        }
    }
}
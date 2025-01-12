using PrimalLike.Components;
using PrimalLike.Content;
using PrimalLike.Graphics;
using PrimalLike.Platform;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace PrimalLike
{
    /// <summary>
    /// Application base class.
    /// </summary>
    public abstract class Application
    {
        /// <summary>
        /// Render surface structure.
        /// </summary>
        struct RenderSurface
        {
            /// <summary>
            /// Window
            /// </summary>
            public PlatformWindow Window;
            /// <summary>
            /// Surface
            /// </summary>
            public ISurface Surface;
        }

        /// <summary>
        /// Gets the current application.
        /// </summary>
        public static Application Current { get; private set; }

        private readonly IPlatform platform;
        private readonly Time time = new();
        private readonly object tickLock = new();
        private readonly List<RenderSurface> renderSurfaces = [];
        private readonly string contentFilename;

        public event EventHandler OnInitialize;
        public event EventHandler OnShutdown;

        /// <summary>
        /// Gets whether the application is running.
        /// </summary>
        public bool IsRunning { get; private set; }
        /// <summary>
        /// Gets whether the application is exiting.
        /// </summary>
        public bool IsExiting { get; private set; }

        /// <summary>
        /// Creates a new application.
        /// </summary>
        /// <param name="platformFactory">Platform factory</param>
        /// <param name="graphicsFactory">Graphics factory</param>
        protected Application(string contentFilename, IPlatformFactory platformFactory, IGraphicsPlatformFactory graphicsFactory)
        {
            this.contentFilename = contentFilename;

            platform = platformFactory.CreatePlatform();

            Renderer.Initialize(graphicsFactory);

            OnInitialize?.Invoke(this, EventArgs.Empty);

            Current = this;
        }

        /// <summary>
        /// Load engine shaders
        /// </summary>
        /// <param name="shadersBlob">Shaders blob</param>
        public static bool LoadEngineShaders(out byte[] shadersBlob)
        {
            string path = Renderer.GetEngineShaderPath();

            return ContentLoader.LoadEngineShaders(path, out shadersBlob);
        }

        /// <summary>
        /// Creates a new window.
        /// </summary>
        /// <param name="info">Initialization info</param>
        /// <returns>Returns the create window</returns>
        public PlatformWindow CreateWindow(IPlatformWindowInfo info)
        {
            var wnd = platform.CreateWindow(info);
            var surface = Renderer.CreateSurface(wnd);
            renderSurfaces.Add(new() { Window = wnd, Surface = surface });

            return wnd;
        }
        /// <summary>
        /// Removes a window.
        /// </summary>
        /// <param name="window">Window to remove</param>
        public void RemoveWindow(PlatformWindow window)
        {
            var rs = renderSurfaces.Find(x => x.Window == window);
            renderSurfaces.Remove(rs);

            Renderer.RemoveSurface(rs.Surface.Id);
            rs.Surface.Dispose();
            platform.RemoveWindow(window);
        }
        /// <summary>
        /// Resizes a window.
        /// </summary>
        /// <param name="window">Window to resize</param>
        /// <param name="clientArea">Client area</param>
        public void ResizeWindow(PlatformWindow window, Rectangle clientArea)
        {
            window.Resized(clientArea);

            var surface = renderSurfaces.Find(x => x.Window == window);
            Renderer.ResizeSurface(surface.Surface.Id, clientArea.Width, clientArea.Height);
        }

        /// <summary>
        /// Creates a camera
        /// </summary>
        /// <param name="info">Camera initialization info</param>
        public Camera CreateCamera(CameraInitInfo info)
        {
            return Renderer.CreateCamera(info);
        }
        /// <summary>
        /// Removes a camera.
        /// </summary>
        /// <param name="id">Camera id</param>
        public void RemoveCamera(CameraId id)
        {
            Renderer.RemoveCamera(id);
        }

        /// <summary>
        /// Runs the application.
        /// </summary>
        public void Run()
        {
            if (IsRunning)
            {
                return;
            }

            IsRunning = true;
            LoadContentAsync();

            time.Update();

            platform.Run();

            OnShutdown?.Invoke(this, EventArgs.Empty);
            ContentLoader.UnloadGame();
            Renderer.Shutdown();
        }
        /// <summary>
        /// Loads content asynchronously.
        /// </summary>
        protected virtual Task LoadContentAsync()
        {
            ContentLoader.LoadGame(contentFilename);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Ticks the application.
        /// </summary>
        public void Tick()
        {
            lock (tickLock)
            {
                if (IsExiting)
                {
                    CheckEndRun();
                    return;
                }

                try
                {
                    time.Update();

                    Script.Update(time.DeltaTime);

                    Update(time);

                    if (BeginDraw())
                    {
                        Draw(time);
                    }
                }
                finally
                {
                    EndDraw();

                    CheckEndRun();
                }
            }
        }
        private void CheckEndRun()
        {
            if (IsExiting && IsRunning)
            {
                time.Stop();

                IsRunning = false;
            }
        }
        /// <summary>
        /// Updates the application.
        /// </summary>
        /// <param name="time">Time</param>
        protected virtual void Update(Time time)
        {

        }
        /// <summary>
        /// Begins drawing.
        /// </summary>
        protected virtual bool BeginDraw()
        {
            return true;
        }
        /// <summary>
        /// Draws the application.
        /// </summary>
        /// <param name="time">Time</param>
        protected virtual void Draw(Time time)
        {
            FpsTimer.Begin();
            foreach (var rs in renderSurfaces)
            {
                Renderer.RenderSurface(rs.Surface.Id);
            }
            FpsTimer.End();
        }
        /// <summary>
        /// Ends drawing.
        /// </summary>
        protected virtual void EndDraw()
        {

        }

        /// <summary>
        /// Exits.
        /// </summary>
        public void Exit()
        {
            if (!IsRunning)
            {
                return;
            }

            IsExiting = true;
        }
    }
}

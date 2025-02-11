using PrimalLike.Common;
using PrimalLike.Components;
using PrimalLike.Content;
using PrimalLike.EngineAPI;
using PrimalLike.Graphics;
using PrimalLike.Platform;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PrimalLike
{
    /// <summary>
    /// Application base class.
    /// </summary>
    public abstract class Application
    {
        /// <summary>
        /// Gets the current application.
        /// </summary>
        public static Application Current { get; private set; }

        private static readonly List<RenderComponent> renderSurfaces = [];

        private readonly Time time = new();
        private readonly object tickLock = new();
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

            PlatformBase.Initialize(platformFactory);

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
        /// Creates a render component.
        /// </summary>
        /// <typeparam name="T">Render component type</typeparam>
        /// <param name="info">Window info</param>
        public static T CreateRenderComponent<T>(IPlatformWindowInfo info) where T : RenderComponent
        {
            T renderComponent = (T)Activator.CreateInstance(typeof(T), info);

            renderSurfaces.Add(renderComponent);

            return renderComponent;
        }
        /// <summary>
        /// Removes a render component.
        /// </summary>
        /// <param name="renderComponent">Render component to remove</param>
        public static void RemoveRenderComponent(RenderComponent renderComponent)
        {
            renderComponent.Remove();
            renderSurfaces.Remove(renderComponent);
        }

        /// <summary>
        /// Creates a render surface.
        /// </summary>
        /// <param name="info">Window init info</param>
        public static RenderSurface CreateRenderSurface(IPlatformWindowInfo info)
        {
            var window = CreateWindow(info);
            var surface = CreateSurface(window);

            return new RenderSurface()
            {
                Window = window,
                Surface = surface
            };
        }
        /// <summary>
        /// Removes a render surface.
        /// </summary>
        /// <param name="renderSurface">Render surface</param>
        public static void RemoveRenderSurface(RenderSurface renderSurface)
        {
            if (renderSurface.Surface.IsValid)
            {
                RemoveSurface(renderSurface.Surface.Id);
            }
            if (renderSurface.Window.IsValid)
            {
                RemoveWindow(renderSurface.Window.Id);
            }
        }

        /// <summary>
        /// Creates a new window.
        /// </summary>
        /// <param name="info">Initialization info</param>
        /// <returns>Returns the create window</returns>
        public static Window CreateWindow(IPlatformWindowInfo info)
        {
            return PlatformBase.CreateWindow(info);
        }
        /// <summary>
        /// Removes a window.
        /// </summary>
        /// <param name="window">Window to remove</param>
        public static void RemoveWindow(uint id)
        {
            Debug.Assert(IdDetail.IsValid(id));
            PlatformBase.RemoveWindow(id);
        }

        /// <summary>
        /// Creates a surface.
        /// </summary>
        /// <param name="window">Window</param>
        public static Surface CreateSurface(Window window)
        {
            return Renderer.CreateSurface(window);
        }
        /// <summary>
        /// Removes a surface.
        /// </summary>
        /// <param name="id">Surface id</param>
        public static void RemoveSurface(SurfaceId id)
        {
            Debug.Assert(IdDetail.IsValid(id));
            Renderer.RemoveSurface(id);
        }

        /// <summary>
        /// Creates a camera
        /// </summary>
        /// <param name="info">Camera initialization info</param>
        public static Camera CreateCamera(CameraInitInfo info)
        {
            return Renderer.CreateCamera(info);
        }
        /// <summary>
        /// Removes a camera.
        /// </summary>
        /// <param name="id">Camera id</param>
        public static void RemoveCamera(CameraId id)
        {
            Debug.Assert(IdDetail.IsValid(id));
            Renderer.RemoveCamera(id);
        }

        /// <summary>
        /// Creates a light.
        /// </summary>
        /// <param name="info">Light info</param>
        public static Light CreateLight(LightInitInfo info)
        {
            return Renderer.CreateLight(info);
        }
        /// <summary>
        /// Removes a light.
        /// </summary>
        /// <param name="id">Light id</param>
        /// <param name="lightSetKey">Lightset key</param>
        public static void RemoveLight(LightId id, ulong lightSetKey)
        {
            Renderer.RemoveLight(id, lightSetKey);
        }

        /// <summary>
        /// Creates a game entity
        /// </summary>
        /// <param name="info">Entity info</param>
        public static Entity CreateEntity(EntityInfo info)
        {
            return GameEntity.Create(info);
        }
        /// <summary>
        /// Removes a game entity.
        /// </summary>
        /// <param name="id">Entity id</param>
        public static void RemoveEntity(EntityId id)
        {
            Debug.Assert(IdDetail.IsValid(id));
            GameEntity.Remove(id);
        }

        /// <summary>
        /// Registers a script.
        /// </summary>
        /// <typeparam name="T">Script type</typeparam>
        public static bool RegisterScript<T>() where T : EntityScript
        {
            return Script.RegisterScript(
                IdDetail.StringHash<T>(),
                (entity) => (T)Activator.CreateInstance(typeof(T), [entity]));
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

            PlatformBase.Run();

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
                rs.Render(time);
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

using Engine.Graphics;
using Engine.Platform;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Engine
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

        private readonly PlatformBase platform;
        private readonly GraphicsBase graphics;
        private readonly Time time = new();
        private readonly object tickLock = new();
        private readonly List<PlatformWindow> windows = [];

        /// <summary>
        /// Gets or sets a value indicating whether vertical sync is enabled.
        /// </summary>
        public bool VerticalSync { get; set; } = true;
        /// <summary>
        /// Gets whether the application is running.
        /// </summary>
        public bool IsRunning { get; private set; }
        /// <summary>
        /// Gets whether the application is exiting.
        /// </summary>
        public bool IsExiting { get; private set; }
        /// <summary>
        /// Gets the main window.
        /// </summary>
        public PlatformWindow MainWindow
        {
            get
            {
                return platform.MainWindow;
            }
        }
        /// <summary>
        /// Gets the aspect ratio of the main window.
        /// </summary>
        public float AspectRatio
        {
            get
            {
                return MainWindow.AspectRatio;
            }
        }

        /// <summary>
        /// Creates a new application.
        /// </summary>
        /// <param name="platformFactory">Platform factory</param>
        /// <param name="graphicsFactory">Graphics factory</param>
        protected Application(IPlatformFactory platformFactory, IGraphicsFactory graphicsFactory)
        {
            platform = platformFactory.CreatePlatform();
            graphics = graphicsFactory.CreateGraphics();

            Current = this;
        }

        /// <summary>
        /// Creates a new window.
        /// </summary>
        /// <param name="info">Initialization info</param>
        /// <returns>Returns the create window</returns>
        public PlatformWindow CreateWindow(IPlatformWindowInfo info)
        {
            var wnd = platform.CreateWindow(info);
            windows.Add(wnd);
            return wnd;
        }
        /// <summary>
        /// Removes a window.
        /// </summary>
        /// <param name="window">Window to remove</param>
        public void RemoveWindow(PlatformWindow window)
        {
            windows.Remove(window);
            platform.RemoveWindow(window);
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
            Initialize();
            LoadContentAsync();

            time.Update();

            platform.Run();

            Shutdown();
        }
        /// <summary>
        /// Initializes the application.
        /// </summary>
        protected virtual void Initialize()
        {
            graphics.Initialize();
        }
        /// <summary>
        /// Loads content asynchronously.
        /// </summary>
        protected virtual Task LoadContentAsync()
        {
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
            graphics.Render();
        }
        /// <summary>
        /// Ends drawing.
        /// </summary>
        protected virtual void EndDraw()
        {

        }

        /// <summary>
        /// Shuts down the application.
        /// </summary>
        protected virtual void Shutdown()
        {
            graphics.Shutdown();
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

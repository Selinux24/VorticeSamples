﻿using Engine.Graphics;
using System.Threading.Tasks;

namespace Engine
{
    public abstract class Application
    {
        public static Application Current { get; private set; }

        private readonly Platform platform;
        private readonly Graphics.Graphics graphics;
        private readonly Time time = new();
        private readonly object tickLock = new();

        public bool VerticalSync { get; set; } = true;
        public bool IsRunning { get; private set; }
        public bool IsExiting { get; private set; }

        public Window MainWindow
        {
            get
            {
                return platform.MainWindow;
            }
        }
        public float AspectRatio
        {
            get
            {
                return MainWindow.AspectRatio;
            }
        }

        protected Application(IPlatformFactory platformFactory, IGraphicsFactory graphicsFactory)
        {
            platform = platformFactory.CreatePlatform();
            graphics = graphicsFactory.CreateGraphics();

            Current = this;
        }

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
        }
        protected virtual void Initialize()
        {

        }
        protected virtual Task LoadContentAsync()
        {
            return Task.CompletedTask;
        }

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
        protected virtual void Update(Time time)
        {

        }
        protected virtual bool BeginDraw()
        {
            return true;
        }
        protected virtual void Draw(Time time)
        {

        }
        protected virtual void EndDraw()
        {

        }

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

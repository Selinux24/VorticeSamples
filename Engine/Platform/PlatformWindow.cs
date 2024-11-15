using System;
using System.Drawing;

namespace Engine.Platform
{
    public abstract class PlatformWindow
    {
        const string DefaultTitle = "Engine";

        private string title = DefaultTitle;
        private bool fullScreen = false;
        private Rectangle clientArea = new(0, 0, 800, 600);

        public event EventHandler ClientAreaChanged;
        public event EventHandler TitleChanged;
        public event EventHandler FullScreenChanged;

        public abstract nint Handle { get; }
        public int BackBufferCount { get; } = 2;
        public Rectangle ClientArea
        {
            get
            {
                return clientArea;
            }
            set
            {
                if (clientArea != value)
                {
                    clientArea = value;
                    SetClientArea(clientArea);
                    OnClientAreaChanged();
                }
            }
        }
        public float AspectRatio
        {
            get
            {
                return clientArea.Width / clientArea.Height;
            }
        }
        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                if (title != value)
                {
                    title = value;
                    SetTitle(title);
                    OnTitleChanged();
                }
            }
        }
        public bool FullScreen
        {
            get
            {
                return fullScreen;
            }
            set
            {
                if (fullScreen != value)
                {
                    fullScreen = value;
                    SetFullScreen(fullScreen);
                    OnFullScreenChanged();
                }
            }
        }

        private void OnClientAreaChanged()
        {
            ClientAreaChanged?.Invoke(this, EventArgs.Empty);
        }
        private void OnTitleChanged()
        {
            TitleChanged?.Invoke(this, EventArgs.Empty);
        }
        private void OnFullScreenChanged()
        {
            FullScreenChanged?.Invoke(this, EventArgs.Empty);
        }

        protected abstract void SetClientArea(Rectangle clientArea);
        protected abstract void SetTitle(string title);
        protected abstract void SetFullScreen(bool fullScreen);

        public void Resized(Rectangle clientArea)
        {
            this.clientArea = clientArea;
        }
    }
}

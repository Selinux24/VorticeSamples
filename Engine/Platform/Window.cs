using System;
using System.Drawing;

namespace Engine.Platform
{
    public abstract class Window
    {
        private string title = "Engine";
        private bool fullScreen = false;
        private SizeF clientSize = new(800, 600);

        public abstract nint Handle { get; }
        public int BackBufferCount { get; } = 2;
        public abstract Rectangle Bounds { get; set; }
        public SizeF ClientSize
        {
            get
            {
                return GetSize();
            }
            set
            {
                if (clientSize != value)
                {
                    clientSize = value;
                    SetSize(clientSize);
                    OnSizeChanged();
                }
            }
        }
        public float AspectRatio
        {
            get
            {
                return clientSize.Width / clientSize.Height;
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


        public event EventHandler SizeChanged;
        public event EventHandler TitleChanged;
        public event EventHandler FullScreenChanged;

        protected void OnSizeChanged()
        {
            SizeChanged?.Invoke(this, EventArgs.Empty);
        }
        protected void OnTitleChanged()
        {
            TitleChanged?.Invoke(this, EventArgs.Empty);
        }
        protected void OnFullScreenChanged()
        {
            FullScreenChanged?.Invoke(this, EventArgs.Empty);
        }

        protected abstract SizeF GetSize();
        protected abstract void SetSize(SizeF size);
        protected abstract void SetSize(Rectangle area);
        protected abstract void SetTitle(string title);
        protected abstract void SetFullScreen(bool fullScreen);
    }
}

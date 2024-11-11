using System;
using System.Drawing;

namespace Engine.Platform
{
    public abstract class Window
    {
        private string title = "Engine";
        private bool fullScreen = false;

        public abstract nint Handle { get; }
        public abstract SizeF ClientSize { get; }
        public abstract Rectangle Bounds { get; }
        public float AspectRatio
        {
            get
            {
                return ClientSize.Width / ClientSize.Height;
            }
        }
        public int BackBufferCount { get; } = 2;
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
                    OnFullScreenChanged();
                }
            }
        }


        public event EventHandler SizeChanged;

        protected virtual void OnSizeChanged()
        {
            SizeChanged?.Invoke(this, EventArgs.Empty);
        }
        protected virtual void OnTitleChanged()
        {
        }
        protected virtual void OnFullScreenChanged()
        {
        }

        public virtual void Resize(SizeF size)
        {
        }
    }
}

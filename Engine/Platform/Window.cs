using System;
using System.Drawing;

namespace Engine.Platform
{
    public abstract class Window
    {
        public abstract nint Handle { get; }
        public abstract string Title { get; set; }
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


        public event EventHandler SizeChanged;

        protected virtual void OnSizeChanged()
        {
            SizeChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}

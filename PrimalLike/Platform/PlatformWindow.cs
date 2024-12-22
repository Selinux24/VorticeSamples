using System;
using System.Drawing;

namespace PrimalLike.Platform
{
    /// <summary>
    /// Represents a platform window.
    /// </summary>
    public abstract class PlatformWindow
    {
        const string DefaultTitle = "PrimalLike Engine";

        private string title = DefaultTitle;
        private bool fullScreen = false;
        private Rectangle clientArea = new(0, 0, 800, 600);

        /// <summary>
        /// Occurs when the client area of the window changes.
        /// </summary>
        public event EventHandler ClientAreaChanged;
        /// <summary>
        /// Occurs when the title of the window changes.
        /// </summary>
        public event EventHandler TitleChanged;
        /// <summary>
        /// Occurs when the full screen state of the window changes.
        /// </summary>
        public event EventHandler FullScreenChanged;

        /// <summary>
        /// Gets the handle of the window.
        /// </summary>
        public abstract nint Handle { get; }
        /// <summary>
        /// Gets the number of back buffers.
        /// </summary>
        public int BackBufferCount { get; } = 2;
        /// <summary>
        /// Gets the client area of the window.
        /// </summary>
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
        /// <summary>
        /// Gets the width of the window.
        /// </summary>
        public int Width { get => clientArea.Width; }
        /// <summary>
        /// Gets the height of the window.
        /// </summary>
        public int Height { get => clientArea.Height; }
        /// <summary>
        /// Gets the aspect ratio of the window.
        /// </summary>
        public float AspectRatio
        {
            get
            {
                return clientArea.Width / clientArea.Height;
            }
        }
        /// <summary>
        /// Gets or sets the title of the window.
        /// </summary>
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
        /// <summary>
        /// Gets or sets a value indicating whether the window is in full screen mode.
        /// </summary>
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
        /// <summary>
        /// Gets or sets a value indicating whether the window is closed.
        /// </summary>
        public bool IsClosed { get; set; }

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

        /// <summary>
        /// Sets the client area of the window.
        /// </summary>
        /// <param name="clientArea">Client area</param>
        protected abstract void SetClientArea(Rectangle clientArea);
        /// <summary>
        /// Sets the title of the window.
        /// </summary>
        /// <param name="title">Title</param>
        protected abstract void SetTitle(string title);
        /// <summary>
        /// Sets the full screen state of the window.
        /// </summary>
        /// <param name="fullScreen">Full screen</param>
        protected abstract void SetFullScreen(bool fullScreen);

        /// <summary>
        /// Resizes the window.
        /// </summary>
        /// <param name="clientArea">New client area</param>
        public void Resized(Rectangle clientArea)
        {
            this.clientArea = clientArea;
        }
    }
}

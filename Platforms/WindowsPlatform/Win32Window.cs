using PrimalLike.Platform;
using System;

namespace WindowsPlatform
{
    /// <summary>
    /// Win32 window.
    /// </summary>
    /// <param name="hwnd">Window handler</param>
    public class Win32Window(nint hwnd, ClientArea clientArea) : IPlatformWindow
    {
        private readonly IntPtr hwnd = hwnd;
        private bool isFullScreen;
        private ClientArea clientArea = clientArea;
        private ClientArea prevClientArea = new();

        /// <inheritdoc />
        public uint Id { get; set; }
        /// <inheritdoc />
        public nint Handle
        {
            get
            {
                return hwnd;
            }
        }
        /// <inheritdoc />
        public bool IsFullscreen
        {
            get => isFullScreen;
            set
            {
                SetFullscreen(isFullScreen);
            }
        }
        /// <inheritdoc />
        public uint Width
        {
            get => clientArea.Width;
            set
            {
                Resize(value, clientArea.Height);
            }
        }
        /// <inheritdoc />
        public uint Height
        {
            get => clientArea.Height;
            set
            {
                Resize(clientArea.Width, value);
            }
        }
        /// <inheritdoc />
        public bool IsClosed { get; internal set; }
        /// <inheritdoc />
        public ClientArea ClientArea
        {
            get => clientArea;
            set
            {
                Resize(value.Width, value.Height);
            }
        }

        /// <inheritdoc />
        public void Show(bool maximize = false)
        {
            Win32Platform.Show(hwnd, maximize);
        }
        /// <inheritdoc />
        public void SetFullscreen(bool fullScreen)
        {
            if (isFullScreen == fullScreen)
            {
                return;
            }

            isFullScreen = fullScreen;

            if (isFullScreen)
            {
                //Save the current window size
                prevClientArea = ClientArea;

                //Remove all styles
                Win32Platform.SetFullScreenStyle(hwnd, isFullScreen);

                //Maximize the window
                Show(true);
            }
            else
            {
                //Restore the window styles
                Win32Platform.SetFullScreenStyle(hwnd, isFullScreen);

                //Restore the window to its previous size
                ClientArea = prevClientArea;

                //Show the window
                Show();
            }
        }
        /// <inheritdoc />
        public void SetCaption(string caption)
        {
            Win32Platform.SetWindowTitle(hwnd, caption);
        }
        /// <inheritdoc />
        public void Resize(uint width, uint height)
        {
            if (clientArea.Width == width && clientArea.Height == height)
            {
                return;
            }

            clientArea.Width = width;
            clientArea.Height = height;

            Win32Platform.SetWindowBounds(hwnd, clientArea);
        }

        /// <summary>
        /// Updates the client area without resizing the window.
        /// </summary>
        /// <param name="clientArea">Client area</param>
        internal void Resized(ClientArea clientArea)
        {
            this.clientArea = clientArea;
        }
    }
}

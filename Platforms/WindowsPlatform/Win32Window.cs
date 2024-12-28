using PrimalLike.Platform;
using System;
using System.Drawing;

namespace WindowsPlatform
{
    /// <summary>
    /// Win32 window.
    /// </summary>
    /// <param name="hwnd">Window handler</param>
    public class Win32Window(nint hwnd) : PlatformWindow()
    {
        private readonly IntPtr hwnd = hwnd;
        private Rectangle prevClientArea = new();

        /// <inheritdoc />
        public override nint Handle
        {
            get
            {
                return hwnd;
            }
        }

        /// <inheritdoc />
        public override void Show(bool maximize = false)
        {
            Win32Platform.Show(hwnd, maximize);
        }

        /// <inheritdoc />
        protected override void SetClientArea(Rectangle clientArea)
        {
            Win32Platform.SetWindowBounds(hwnd, clientArea);
        }
        /// <inheritdoc />
        protected override void SetTitle(string title)
        {
            Win32Platform.SetWindowTitle(hwnd, title);
        }
        /// <inheritdoc />
        protected override void SetFullScreen(bool fullScreen)
        {
            if (fullScreen)
            {
                //Save the current window size
                prevClientArea = ClientArea;

                //Remove all styles
                Win32Platform.SetFullScreenStyle(hwnd, fullScreen);

                //Maximize the window
                Show(true);
            }
            else
            {
                //Restore the window styles
                Win32Platform.SetFullScreenStyle(hwnd, fullScreen);

                //Restore the window to its previous size
                ClientArea = prevClientArea;

                //Show the window
                Show();
            }
        }
    }
}

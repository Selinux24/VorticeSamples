using Engine.Platform;
using System;
using System.Drawing;
using static WindowsPlatform.Native.User32;

namespace WindowsPlatform
{
    public class Win32Window(nint hwnd) : Window()
    {
        private readonly IntPtr hwnd = hwnd;
        private Rectangle clientArea = new() { X = 0, Y = 0, Width = 1920, Height = 1080 };

        /// <inheritdoc />
        public override nint Handle
        {
            get
            {
                return hwnd;
            }
        }
        /// <inheritdoc />
        public override Rectangle Bounds
        {
            get
            {
                GetWindowRect(hwnd, out var windowRect);
                return new Rectangle(
                    windowRect.Left,
                    windowRect.Top,
                    windowRect.GetWidth(),
                    windowRect.GetHeight());
            }
            set
            {
                SetSize(value);
            }
        }

        public void Show(bool maximize = false)
        {
            var nCmdShow = maximize ? ShowWindowCommands.SW_MAXIMIZE : ShowWindowCommands.SW_SHOWNORMAL;
            ShowWindow(hwnd, nCmdShow);
            UpdateWindow(hwnd);
        }
        public void Destroy()
        {
            DestroyWindow(hwnd);
        }

        protected override SizeF GetSize()
        {
            var area = FullScreen ? new() : clientArea;

            return new(area.Bottom - area.Top, area.Right - area.Left);
        }
        protected override void SetSize(SizeF size)
        {
            var area = FullScreen ? new() : clientArea;
            area.Height = (int)size.Height;
            area.Width = (int)size.Width;
            SetSize(area);
        }
        protected override void SetSize(Rectangle area)
        {
            var windowRect = new RECT
            {
                Left = area.Left,
                Top = area.Top,
                Right = area.Left + area.Width,
                Bottom = area.Top + area.Height
            };
            AdjustWindowRect(
                ref windowRect,
                WindowStyles.WS_VISIBLE,
                false);

            MoveWindow(
                hwnd,
                windowRect.Left,
                windowRect.Top,
                windowRect.GetWidth(),
                windowRect.GetHeight(),
                true);
        }

        protected override void SetTitle(string title)
        {
            SetWindowTextW(hwnd, title);
        }

        protected override void SetFullScreen(bool fullScreen)
        {
            if (fullScreen)
            {
                GetClientRect(hwnd, out var area);
                clientArea = new(area.Left, area.Top, area.GetWidth(), area.GetHeight());

                SetWindowLongPtrW(
                    hwnd,
                    WindowLongIndex.GWL_STYLE,
                    0);

                Show(true);
            }
            else
            {
                SetWindowLongPtrW(
                    hwnd,
                    WindowLongIndex.GWL_STYLE,
                    (nint)WindowStyles.WS_VISIBLE);

                SetSize(clientArea);
                Show();
            }
        }
    }
}

using Engine.Platform;
using System;
using System.Drawing;
using static WindowsPlatform.Native.User32;

namespace WindowsPlatform
{
    public class Win32Window(nint hwnd) : Window()
    {
        private readonly IntPtr hwnd = hwnd;

        /// <inheritdoc />
        public override nint Handle
        {
            get
            {
                return hwnd;
            }
        }
        /// <inheritdoc />
        public override SizeF ClientSize
        {
            get
            {
                GetClientRect(hwnd, out var windowRect);
                return new(
                    MathF.Max(1.0f, windowRect.Right - windowRect.Left),
                    MathF.Max(1.0f, windowRect.Bottom - windowRect.Top));
            }
        }
        /// <inheritdoc />
        public override Rectangle Bounds
        {
            get
            {
                GetWindowRect(hwnd, out var windowRect);
                return new Rectangle(
                    windowRect.Left, windowRect.Top,
                    windowRect.Right - windowRect.Left,
                    windowRect.Bottom - windowRect.Top);
            }
        }

        public void Show()
        {
            ShowWindow(hwnd, 1);
        }
        public void Destroy()
        {
            DestroyWindow(hwnd);
        }
    }
}

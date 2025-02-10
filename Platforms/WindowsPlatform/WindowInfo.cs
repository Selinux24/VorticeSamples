using System;
using System.Drawing;
using static Native32.User32;

namespace WindowsPlatform
{
    record WindowInfo()
    {
        public IntPtr Hwnd = IntPtr.Zero;
        public RECT ClientArea = new() { Left = 0, Top = 0, Right = 1920, Bottom = 1080 };
        public RECT FullScreenArea = new();
        public Point TopLeft = new(0, 0);
        public WindowStyles Style = WindowStyles.WS_VISIBLE;
        public bool IsFullScreen = false;
        public bool IsClosed = false;
    }
}

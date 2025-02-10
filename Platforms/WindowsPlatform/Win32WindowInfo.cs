using PrimalLike.Platform;
using System;

namespace WindowsPlatform
{
    public struct Win32WindowInfo() : IPlatformWindowInfo
    {
        public string Caption { get; set; } = "Engine";
        public ClientArea ClientArea { get; set; } = new() { Left = 0, Top = 0, Width = 1920, Height = 1080 };
        public bool IsFullScreen { get; set; } = false;
        public WndProcDelegate Callback { get; set; } = null;
        public IntPtr Parent { get; set; } = IntPtr.Zero;
    }
}

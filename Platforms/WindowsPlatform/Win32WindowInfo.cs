using PrimalLike.Platform;

namespace WindowsPlatform
{
    public struct Win32WindowInfo() : IPlatformWindowInfo
    {
        public string Title { get; set; } = "Engine";
        public ClientArea ClientArea { get; set; } = new() { Left = 0, Top = 0, Width = 1920, Height = 1080 };
        public bool IsFullScreen { get; set; } = false;
        public WndProcDelegate WndProc { get; set; } = null;
    }
}

using System.Drawing;

namespace Engine.Platform
{
    public struct PlatformWindowInfo()
    {
        public string Title { get; set; } = "Engine";
        public Rectangle ClientArea { get; set; } = new() { X = 0, Y = 0, Width = 1920, Height = 1080 };
        public bool IsFullScreen { get; set; } = false;
    }
}

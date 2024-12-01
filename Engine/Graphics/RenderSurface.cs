using Engine.Platform;

namespace Engine.Graphics
{
    public struct RenderSurface
    {
        public PlatformWindow Window { get; set; }
        public ISurface Surface { get; set; }
    }
}

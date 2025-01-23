global using WindowId = System.UInt32;
using PrimalLike.Common;
using System.Diagnostics;

namespace PrimalLike.Platform
{
    public class Window
    {
        private WindowId id = WindowId.MaxValue;

        public Window()
        {

        }
        public Window(WindowId id)
        {
            this.id = id;
        }

        public WindowId Id { get => id; internal set => id = value; }
        public bool IsValid { get => IdDetail.IsValid(id); }

        public bool IsFullscreen { get => PlatformBase.IsWindoFullscreen(id); set => SetFullscreen(value); }
        public nint Handle { get => PlatformBase.GetWindowHandle(id); }
        public uint Width { get => PlatformBase.GetWindowWidth(id); }
        public uint Height { get => PlatformBase.GetWindowHeight(id); }
        public bool IsClosed { get => PlatformBase.IsWindowClosed(id); }

        public void SetFullscreen(bool isFullscreen)
        {
            Debug.Assert(IsValid);
            PlatformBase.SetFullscreen(id, isFullscreen);
        }
        public void SetCaption(string caption)
        {
            Debug.Assert(IsValid);
            PlatformBase.SetCaption(id, caption);
        }
        public void Resize(uint width, uint height)
        {
            Debug.Assert(IsValid);
            PlatformBase.Resize(id, width, height);
        }
    }
}

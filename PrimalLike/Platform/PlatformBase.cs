using PrimalLike.EngineAPI;

namespace PrimalLike.Platform
{
    static class PlatformBase
    {
        private static IPlatform platform;

        public static bool Initialize(IPlatformFactory platformFactory)
        {
            platform = platformFactory.CreatePlatform();

            return true;
        }

        public static Window CreateWindow(IPlatformWindowInfo info)
        {
            return platform.CreateWindow(info);
        }
        public static void RemoveWindow(WindowId id)
        {
            platform.RemoveWindow(id);
        }

        public static nint GetWindowHandle(WindowId id)
        {
            return platform.GetWindowHandle(id);
        }
        public static void SetFullscreen(WindowId id, bool isFullscreen)
        {
            platform.SetFullscreen(id, isFullscreen);
        }
        public static bool IsWindoFullscreen(WindowId id)
        {
            return platform.IsFullscreen(id);
        }
        public static void SetCaption(WindowId id, string caption)
        {
            platform.SetCaption(id, caption);
        }
        public static void Resize(WindowId id, uint width, uint height)
        {
            platform.Resize(id, width, height);
        }
        public static uint GetWindowWidth(WindowId id)
        {
            return platform.GetWidth(id);
        }
        public static uint GetWindowHeight(WindowId id)
        {
            return platform.GetHeight(id);
        }
        public static bool IsWindowClosed(WindowId id)
        {
            return platform.IsClosed(id);
        }

        public static void Run()
        {
            platform.Run();
        }
    }
}

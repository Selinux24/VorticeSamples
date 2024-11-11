using Engine.Components;
using Engine.Content;
using Engine.Graphics;
using Engine.Platform;

namespace Engine.Core
{
    public static class Engine
    {
        public static RenderSurface GameWindow { get; private set; }

        public static bool EngineInitialize(string path, Window window)
        {
            if (!ContentLoader.LoadGame(path)) return false;

            GameWindow = new RenderSurface
            {
                Window = window,
            };

            return true;
        }

        public static void EngineUpdate(float dt)
        {
            Script.Update(dt);
        }

        public static void EngineShutdown()
        {
            ContentLoader.UnloadGame();
        }
    }
}

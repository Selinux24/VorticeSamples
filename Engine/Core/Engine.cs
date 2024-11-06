using Engine.Components;
using Engine.Content;

namespace Engine.Core
{
    public static class Engine
    {
        public static bool EngineInitialize(string path)
        {
            return ContentLoader.LoadGame(path);
        }

        public static void EngineUpdate(float dt)
        {
            ScriptComponent.Update(dt);
        }

        public static void EngineShutdown()
        {
            ContentLoader.UnloadGame();
        }
    }
}

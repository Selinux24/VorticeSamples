using Engine.Components;
using Engine.Content;

namespace Engine.Core
{
    /// <summary>
    /// Engine
    /// </summary>
    public static class Engine
    {
        /// <summary>
        /// Initializes the engine
        /// </summary>
        /// <param name="path">Binary path</param>
        public static bool EngineInitialize(string path)
        {
            return ContentLoader.LoadGame(path);
        }
        /// <summary>
        /// Updates the engine state
        /// </summary>
        /// <param name="dt">Delta time in seconds</param>
        public static void EngineUpdate(float dt)
        {
            Script.Update(dt);
        }
        /// <summary>
        /// Shuts down the engine
        /// </summary>
        public static void EngineShutdown()
        {
            ContentLoader.UnloadGame();
        }
    }
}

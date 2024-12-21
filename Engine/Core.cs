using Engine.Components;
using Engine.Content;
using Engine.Graphics;

namespace Engine
{
    /// <summary>
    /// Core
    /// </summary>
    public static class Core
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

        /// <summary>
        /// Load engine shaders
        /// </summary>
        /// <param name="shadersBlob">Shaders blob</param>
        public static bool LoadEngineShaders(out byte[] shadersBlob)
        {
            string path = GraphicsCore.GetEngineShaderPath();

            return ContentLoader.LoadEngineShaders(path, out shadersBlob);
        }
    }
}

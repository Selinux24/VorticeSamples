using System.IO;
using System.Runtime.CompilerServices;

namespace Direct3D12.Hlsl
{
    static class HlslHelper
    {
        public static readonly string EngineShadersDirectory = GetSolutionHlslDirectory();
        public static readonly string EngineIncludesDirectory = GetSolutionHlslDirectory();

        static string GetSolutionHlslDirectory([CallerFilePath] string path = null)
        {
            return Path.GetDirectoryName(path);
        }
    }
}

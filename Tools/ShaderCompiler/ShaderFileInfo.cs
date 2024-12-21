
namespace ShaderCompiler
{
    public class ShaderFileInfo(string fileName, string function, uint type, string profile)
    {
        public string FileName { get; } = fileName;
        public string Function { get; } = function;
        public uint Type { get; } = type;
        public string Profile { get; } = profile;
    }
}

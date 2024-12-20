
namespace ShaderCompiler
{
    class ShaderFileInfo(string fileName, string function, ShaderType type)
    {
        public string FileName { get; } = fileName;
        public string Function { get; } = function;
        public ShaderType Type { get; } = type;
    }
}

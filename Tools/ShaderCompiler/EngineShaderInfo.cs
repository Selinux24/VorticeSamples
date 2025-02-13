
namespace ShaderCompiler
{
    public readonly struct EngineShaderInfo(uint id, ShaderFileInfo info, string[] extraArguments = null)
    {
        public uint Id { get; } = id;
        public ShaderFileInfo Info { get; } = info;
        public string[] ExtraArguments { get; } = extraArguments;
    }
}


namespace ShaderCompiler
{
    public readonly struct EngineShaderInfo(uint id, ShaderFileInfo info)
    {
        public uint Id { get; } = id;
        public ShaderFileInfo Info { get; } = info;
    }
}

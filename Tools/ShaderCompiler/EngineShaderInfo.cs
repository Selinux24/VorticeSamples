
namespace ShaderCompiler
{
    public class EngineShaderInfo(uint id, ShaderFileInfo info)
    {
        public uint Id { get; } = id;
        public ShaderFileInfo Info { get; } = info;
    }
}

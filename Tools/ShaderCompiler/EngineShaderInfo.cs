
namespace ShaderCompiler
{
    class EngineShaderInfo(EngineShader id, ShaderFileInfo info)
    {
        public EngineShader Id { get; } = id;
        public ShaderFileInfo Info { get; } = info;
    }
}

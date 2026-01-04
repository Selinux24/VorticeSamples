
namespace Direct3D12.ShaderCompiler
{
    public readonly struct ShaderInfo(uint id, ShaderFileInfo info, string[] extraArguments = null)
    {
        public uint Id { get; } = id;
        public ShaderFileInfo Info { get; } = info;
        public string[] ExtraArguments { get; } = extraArguments;
    }
}

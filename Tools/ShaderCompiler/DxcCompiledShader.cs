
namespace ShaderCompiler
{
    class DxcCompiledShader(byte[] byteCode = null, byte[] disassembly = null, DxcShaderHash hash = default)
    {
        public byte[] ByteCode { get; } = byteCode;
        public byte[] Disassembly { get; } = disassembly;
        public DxcShaderHash Hash { get; } = hash;
    }
}

using System;

namespace ShaderCompiler
{
    public readonly struct CompiledShader(ReadOnlyMemory<byte> byteCode, ShaderHash hash, byte[] disassembly = null)
    {
        public ReadOnlyMemory<byte> ByteCode { get; } = byteCode;
        public ShaderHash Hash { get; } = hash;
        public byte[] Disassembly { get; } = disassembly;
    }
}


namespace ShaderCompiler
{
    class DxcShaderHash(int flags, byte[] hashDigest)
    {
        public int Flags { get; } = flags;
        public byte[] HashDigest { get; } = hashDigest;
    }
}

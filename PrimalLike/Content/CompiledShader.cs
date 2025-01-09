using System.Runtime.InteropServices;

namespace PrimalLike.Content
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CompiledShader()
    {
        public const uint HashLength = 16;
        public ulong ByteCodeSize;
        public byte[] Hash;
        public byte[] ByteCode;

        public readonly bool IsValid()
        {
            return ByteCode?.Length > 0;
        }
    }
}

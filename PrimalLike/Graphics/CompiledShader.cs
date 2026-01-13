using System;
using System.Runtime.InteropServices;

namespace PrimalLike.Graphics
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CompiledShader
    {
        public const uint HashLength = 16;

        public readonly ReadOnlyMemory<byte> ByteCode;
        public readonly ulong ByteCodeSize;
        public readonly byte[] Hash;

        public CompiledShader(ReadOnlyMemory<byte> byteCode, byte[] hash = null) : this()
        {
            ByteCode = byteCode;
            ByteCodeSize = (ulong)byteCode.Length;
            Hash = hash;
        }

        public readonly bool IsValid()
        {
            return ByteCode.Length > 0;
        }
    }
}

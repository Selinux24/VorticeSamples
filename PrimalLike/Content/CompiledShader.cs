using System;
using System.Runtime.InteropServices;

namespace PrimalLike.Content
{
    [StructLayout(LayoutKind.Sequential)]
    public struct CompiledShader()
    {
        public const uint HashLength = 16;

        public ulong ByteCodeSize;
        public byte[] Hash;
        public ReadOnlyMemory<byte> ByteCode;

        public readonly bool IsValid()
        {
            return ByteCode.Length > 0;
        }
        public readonly IntPtr GetData()
        {
            IntPtr shaderData = Marshal.AllocHGlobal((int)ByteCodeSize);
            Marshal.Copy(ByteCode.ToArray(), 0, shaderData, (int)ByteCodeSize);

            return shaderData;
        }
    }
}

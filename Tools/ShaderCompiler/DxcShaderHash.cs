using System.Runtime.InteropServices;
using System.Text;

namespace ShaderCompiler
{
    [StructLayout(LayoutKind.Sequential)]
    struct DxcShaderHash(uint flags, byte[] hashDigest)
    {
        public uint Flags = flags;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] HashDigest = hashDigest;

        public string GetHashDigestString()
        {
            StringBuilder sb = new();
            for (int i = 0; i < HashDigest.Length; i++)
            {
                sb.Append($"{HashDigest[i]:x2} ");
            }
            return sb.ToString();
        }
    }
}

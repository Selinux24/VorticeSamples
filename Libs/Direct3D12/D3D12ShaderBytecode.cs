using System;
using System.Runtime.InteropServices;

namespace Direct3D12
{
    [StructLayout(LayoutKind.Sequential)]
    struct D3D12ShaderBytecode
    {
        public int ByteCodeLength;
        public IntPtr ByteCode;
    }
}

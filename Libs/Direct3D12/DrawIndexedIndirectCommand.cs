using Direct3D12.Content;
using System;
using System.Runtime.InteropServices;
using Vortice.Direct3D12;

namespace Direct3D12
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    unsafe struct DrawIndexedIndirectCommand
    {
        private fixed ulong parameters[(int)OpaqueRootParameter.Count];

        public readonly Span<ulong> Parameters
        {
            get
            {
                fixed (ulong* ptr = parameters) return new Span<ulong>(ptr, (int)OpaqueRootParameter.Count);
            }
        }
        public IndexBufferView IndexBufferView;
        public DrawIndexedArguments DrawIndexedArgs;

        public ulong GetParameter(OpaqueRootParameter index)
        {
            return parameters[(int)index];
        }
        public void SetParameter(OpaqueRootParameter index, ulong value)
        {
            parameters[(int)index] = value;
        }
    }
}

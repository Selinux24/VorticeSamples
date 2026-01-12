using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Direct3D12.Helpers
{
    static class BuffersHelper
    {
        public static unsafe void WriteUnaligned<T>(T data, IntPtr dst) where T : unmanaged
        {
            Debug.Assert(dst != IntPtr.Zero);

            uint size = (uint)Marshal.SizeOf<T>();

            Unsafe.CopyBlockUnaligned(dst.ToPointer(), Unsafe.AsPointer(ref data), size);
        }
        public static unsafe void WriteUnaligned<T>(T[] data, IntPtr dst) where T : unmanaged
        {
            Debug.Assert(dst != IntPtr.Zero);
            Debug.Assert(data?.Length > 0);

            uint size = (uint)Marshal.SizeOf<T>();

            for (uint i = 0; i < data.Length; i++)
            {
                Unsafe.CopyBlockUnaligned(dst.ToPointer(), Unsafe.AsPointer(ref data[i]), size);

                dst += (IntPtr)size;
            }
        }

        public static unsafe void WriteAligned(IntPtr src, IntPtr dst, ulong srcSizeInBytes, ulong dstSizeInBytes)
        {
            Debug.Assert(src != IntPtr.Zero);
            Debug.Assert(dst != IntPtr.Zero);
            Debug.Assert(srcSizeInBytes > 0);
            Debug.Assert(dstSizeInBytes > 0);

            Buffer.MemoryCopy(src.ToPointer(), dst.ToPointer(), srcSizeInBytes, dstSizeInBytes);
        }
    }
}

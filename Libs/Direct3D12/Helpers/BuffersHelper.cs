using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Direct3D12.Helpers
{
    static class BuffersHelper
    {
        /// <summary>
        /// Writes the specified data to the destination pointer without any alignment requirements. The size of the data is determined by the type parameter T.
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="data">The data to write</param>
        /// <param name="dst">The destination pointer</param>
        /// <returns>The size of the written data in bytes</returns>
        public static unsafe uint WriteUnaligned<T>(T data, IntPtr dst)
        {
            Debug.Assert(dst != IntPtr.Zero);

            uint size = (uint)Marshal.SizeOf<T>();

            Unsafe.CopyBlockUnaligned(dst.ToPointer(), Unsafe.AsPointer(ref data), size);

            return size;
        }
        /// <summary>
        /// Writes the specified data to the destination pointer without any alignment requirements. The size of the data is determined by the type parameter T.
        /// </summary>
        /// <typeparam name="T">Data type</typeparam>
        /// <param name="data">The data to write</param>
        /// <param name="dst">The destination pointer</param>
        /// <returns>The size of the written data in bytes</returns>
        public static unsafe uint WriteUnaligned<T>(T[] data, IntPtr dst)
        {
            Debug.Assert(dst != IntPtr.Zero);
            Debug.Assert(data?.Length > 0);

            uint size = (uint)Marshal.SizeOf<T>();

            for (uint i = 0; i < data.Length; i++)
            {
                Unsafe.CopyBlockUnaligned(dst.ToPointer(), Unsafe.AsPointer(ref data[i]), size);

                dst += (IntPtr)size;
            }

            return size * (uint)data.Length;
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
